// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
	const AUTOCOMPLETE_SELECTOR = "[data-autocomplete-url][data-autocomplete-name]";
	const SUGGEST_SELECTOR = "input[data-suggest-url]";

	const configureValidation = () => {
		const validator = window.jQuery?.validator;
		if (!validator) {
			return;
		}

		validator.setDefaults({
			onfocusout(element) {
				this.element(element);
			},
			onkeyup: false
		});
	};

	const initAutocomplete = (container) => {
		if (container.dataset.autocompleteReady === "true") {
			return;
		}

		container.dataset.autocompleteReady = "true";

		const url = container.dataset.autocompleteUrl;
		const name = container.dataset.autocompleteName;
		const input = container.querySelector(".autocomplete-input") || container.querySelector("input[type='text']");
		const hidden = container.querySelector(`input[type='hidden'][name='${name}']`) || container.querySelector("input[type='hidden']");
		const panel = container.querySelector(".autocomplete-panel");
		const results = container.querySelector("[data-autocomplete-results]");
		const status = container.querySelector("[data-autocomplete-status]");
		const moreButton = container.querySelector("[data-autocomplete-more]");
		const placeholder = container.dataset.autocompletePlaceholder;
		const previewCount = Number.parseInt(container.dataset.autocompletePreviewCount || "4", 10);

		if (!url || !input || !hidden || !panel || !results || !moreButton) {
			return;
		}

		if (placeholder && !input.getAttribute("placeholder")) {
			input.setAttribute("placeholder", placeholder);
		}

		let items = [];
		let activeIndex = -1;
		let debounceTimer = null;
		let abortController = null;
		let isExpanded = false;

		const openPanel = () => {
			panel.classList.add("is-open");
			input.setAttribute("aria-expanded", "true");
			panel.setAttribute("aria-hidden", "false");
		};

		const closePanel = () => {
			panel.classList.remove("is-open");
			input.setAttribute("aria-expanded", "false");
			panel.setAttribute("aria-hidden", "true");
			activeIndex = -1;
			updateActive();
		};

		const setStatus = (message, state) => {
			if (!status) {
				return;
			}

			status.textContent = message;
			status.dataset.state = state || "";
			status.classList.toggle("is-hidden", !message);
		};

		const clearResults = () => {
			results.innerHTML = "";
			items = [];
			activeIndex = -1;
			isExpanded = false;
			moreButton.classList.add("is-hidden");
			moreButton.textContent = "Show more";
		};

		const resetResults = () => {
			results.innerHTML = "";
			activeIndex = -1;
		};

		const updateActive = () => {
			const options = results.querySelectorAll(".autocomplete-option");
			options.forEach((option, index) => {
				const isActive = index === activeIndex;
				option.classList.toggle("is-active", isActive);
				if (isActive) {
					option.scrollIntoView({ block: "nearest" });
				}
			});
		};

		const selectItem = (item) => {
			hidden.value = item.id;
			input.value = item.name;
			closePanel();
		};

		const renderResults = (list) => {
			resetResults();

			if (!list.length) {
				setStatus("No matches found.", "empty");
				return;
			}

			setStatus("", "");
			items = list;
			const maxVisible = Number.isFinite(previewCount) && previewCount > 0 ? previewCount : 4;
			const visibleItems = isExpanded ? list : list.slice(0, maxVisible);

			visibleItems.forEach((item, index) => {
				const listItem = document.createElement("li");
				const button = document.createElement("button");

				button.type = "button";
				button.className = "autocomplete-option";
				button.setAttribute("role", "option");
				button.textContent = item.name;
				button.dataset.id = item.id;
				button.dataset.name = item.name;

				button.addEventListener("click", () => selectItem(item));
				button.addEventListener("mouseenter", () => {
					activeIndex = index;
					updateActive();
				});

				listItem.appendChild(button);
				results.appendChild(listItem);
			});

			if (list.length > maxVisible) {
				moreButton.classList.remove("is-hidden");
				moreButton.textContent = isExpanded
					? "Show fewer"
					: `Show more (${list.length - maxVisible} more)`;
			} else {
				moreButton.classList.add("is-hidden");
				moreButton.textContent = "Show more";
			}
		};

		const fetchResults = (term) => {
			if (abortController) {
				abortController.abort();
			}

			abortController = new AbortController();
			const query = term ? `?q=${encodeURIComponent(term)}` : "";
			setStatus("Searching...", "loading");
			openPanel();

			fetch(`${url}${query}`, {
				signal: abortController.signal,
				headers: {
					Accept: "application/json"
				}
			})
				.then((response) => {
					if (!response.ok) {
						throw new Error("Request failed");
					}

					return response.json();
				})
				.then((data) => {
					if (!Array.isArray(data)) {
						renderResults([]);
						return;
					}

					const list = data.map((item) => ({
						id: item.id,
						name: item.name
					}));
					renderResults(list);
				})
				.catch((error) => {
					if (error.name === "AbortError") {
						return;
					}

					clearResults();
					setStatus("Unable to load results.", "error");
				});
		};

		input.addEventListener("focus", () => {
			openPanel();
			if (!input.value.trim()) {
				setStatus("Start typing to search.", "idle");
			}
		});

		input.addEventListener("input", () => {
			const term = input.value.trim();
			hidden.value = "";
			isExpanded = false;

			if (!term) {
				clearResults();
				setStatus("Start typing to search.", "idle");
				if (abortController) {
					abortController.abort();
				}
				openPanel();
				return;
			}

			clearResults();
			if (debounceTimer) {
				clearTimeout(debounceTimer);
			}

			debounceTimer = setTimeout(() => fetchResults(term), 220);
		});

		input.addEventListener("keydown", (event) => {
			if (!panel.classList.contains("is-open")) {
				return;
			}

			const options = results.querySelectorAll(".autocomplete-option");

			if (event.key === "ArrowDown") {
				event.preventDefault();
				if (!options.length) {
					return;
				}

				activeIndex = Math.min(activeIndex + 1, options.length - 1);
				updateActive();
				return;
			}

			if (event.key === "ArrowUp") {
				event.preventDefault();
				if (!options.length) {
					return;
				}

				activeIndex = Math.max(activeIndex - 1, 0);
				updateActive();
				return;
			}

			if (event.key === "Enter") {
				if (activeIndex >= 0 && options[activeIndex]) {
					event.preventDefault();
					const option = options[activeIndex];
					selectItem({ id: option.dataset.id, name: option.dataset.name });
				}
				return;
			}

			if (event.key === "Escape") {
				event.preventDefault();
				closePanel();
			}
		});

		moreButton.addEventListener("click", () => {
			if (!items.length) {
				return;
			}

			isExpanded = !isExpanded;
			renderResults(items);
			openPanel();
		});

		document.addEventListener("click", (event) => {
			if (!container.contains(event.target)) {
				closePanel();
			}
		});
	};

	const initListSuggest = (input) => {
		if (input.dataset.suggestReady === "true") {
			return;
		}

		input.dataset.suggestReady = "true";

		const url = input.dataset.suggestUrl;
		const previewCount = Number.parseInt(input.dataset.previewCount || "4", 10);
		const wrapper = input.closest(".autocomplete-search") || input.parentElement;

		if (!url || !wrapper) {
			return;
		}

		if (!input.getAttribute("autocomplete")) {
			input.setAttribute("autocomplete", "off");
		}

		input.setAttribute("aria-expanded", "false");

		let panel = wrapper.querySelector(".autocomplete-panel");
		let status = panel?.querySelector("[data-autocomplete-status]") || null;
		let results = panel?.querySelector("[data-autocomplete-results]") || null;
		let moreButton = panel?.querySelector("[data-autocomplete-more]") || null;

		if (!panel) {
			panel = document.createElement("div");
			panel.className = "autocomplete-panel";
			panel.setAttribute("role", "listbox");
			panel.setAttribute("aria-hidden", "true");

			status = document.createElement("div");
			status.className = "autocomplete-status is-hidden";
			status.setAttribute("data-autocomplete-status", "");

			results = document.createElement("ul");
			results.className = "autocomplete-results";
			results.setAttribute("data-autocomplete-results", "");

			moreButton = document.createElement("button");
			moreButton.className = "autocomplete-more is-hidden";
			moreButton.type = "button";
			moreButton.setAttribute("data-autocomplete-more", "");
			moreButton.textContent = "Show more";

			panel.append(status, results, moreButton);
			wrapper.appendChild(panel);
		}

		if (!status || !results || !moreButton) {
			return;
		}

		let items = [];
		let activeIndex = -1;
		let debounceTimer = null;
		let abortController = null;
		let isExpanded = false;

		const openPanel = () => {
			panel.classList.add("is-open");
			input.setAttribute("aria-expanded", "true");
			panel.setAttribute("aria-hidden", "false");
		};

		const closePanel = () => {
			panel.classList.remove("is-open");
			input.setAttribute("aria-expanded", "false");
			panel.setAttribute("aria-hidden", "true");
			activeIndex = -1;
			updateActive();
		};

		const setStatus = (message, state) => {
			status.textContent = message;
			status.dataset.state = state || "";
			status.classList.toggle("is-hidden", !message);
		};

		const clearResults = () => {
			results.innerHTML = "";
			items = [];
			activeIndex = -1;
			isExpanded = false;
			moreButton.classList.add("is-hidden");
			moreButton.textContent = "Show more";
		};

		const resetResults = () => {
			results.innerHTML = "";
			activeIndex = -1;
		};

		const updateActive = () => {
			const options = results.querySelectorAll(".autocomplete-option");
			options.forEach((option, index) => {
				const isActive = index === activeIndex;
				option.classList.toggle("is-active", isActive);
				if (isActive) {
					option.scrollIntoView({ block: "nearest" });
				}
			});
		};

		const selectItem = (item) => {
			input.value = item.name;
			closePanel();
		};

		const renderResults = (list) => {
			resetResults();

			if (!list.length) {
				setStatus("No matches found.", "empty");
				return;
			}

			setStatus("", "");
			items = list;
			const maxVisible = Number.isFinite(previewCount) && previewCount > 0 ? previewCount : 4;
			const visibleItems = isExpanded ? list : list.slice(0, maxVisible);

			visibleItems.forEach((item, index) => {
				const listItem = document.createElement("li");
				const button = document.createElement("button");

				button.type = "button";
				button.className = "autocomplete-option";
				button.setAttribute("role", "option");
				button.textContent = item.name;
				button.dataset.id = item.id;
				button.dataset.name = item.name;

				button.addEventListener("click", () => selectItem(item));
				button.addEventListener("mouseenter", () => {
					activeIndex = index;
					updateActive();
				});

				listItem.appendChild(button);
				results.appendChild(listItem);
			});

			if (list.length > maxVisible) {
				moreButton.classList.remove("is-hidden");
				moreButton.textContent = isExpanded
					? "Show fewer"
					: `Show more (${list.length - maxVisible} more)`;
			} else {
				moreButton.classList.add("is-hidden");
				moreButton.textContent = "Show more";
			}
		};

		const fetchResults = (term) => {
			if (abortController) {
				abortController.abort();
			}

			abortController = new AbortController();
			const query = term ? `?q=${encodeURIComponent(term)}` : "";
			setStatus("Searching...", "loading");
			openPanel();

			fetch(`${url}${query}`, {
				signal: abortController.signal,
				headers: {
					Accept: "application/json"
				}
			})
				.then((response) => {
					if (!response.ok) {
						throw new Error("Request failed");
					}

					return response.json();
				})
				.then((data) => {
					if (!Array.isArray(data)) {
						renderResults([]);
						return;
					}

					const list = data.map((item) => ({
						id: item.id,
						name: item.name
					}));
					renderResults(list);
				})
				.catch((error) => {
					if (error.name === "AbortError") {
						return;
					}

					clearResults();
					setStatus("Unable to load results.", "error");
				});
		};

		input.addEventListener("focus", () => {
			openPanel();
			if (!input.value.trim()) {
				setStatus("Start typing to search.", "idle");
			}
		});

		input.addEventListener("input", () => {
			const term = input.value.trim();
			isExpanded = false;

			if (!term) {
				clearResults();
				setStatus("Start typing to search.", "idle");
				if (abortController) {
					abortController.abort();
				}
				openPanel();
				return;
			}

			clearResults();
			if (debounceTimer) {
				clearTimeout(debounceTimer);
			}

			debounceTimer = setTimeout(() => fetchResults(term), 220);
		});

		input.addEventListener("keydown", (event) => {
			if (!panel.classList.contains("is-open")) {
				return;
			}

			const options = results.querySelectorAll(".autocomplete-option");

			if (event.key === "ArrowDown") {
				event.preventDefault();
				if (!options.length) {
					return;
				}

				activeIndex = Math.min(activeIndex + 1, options.length - 1);
				updateActive();
				return;
			}

			if (event.key === "ArrowUp") {
				event.preventDefault();
				if (!options.length) {
					return;
				}

				activeIndex = Math.max(activeIndex - 1, 0);
				updateActive();
				return;
			}

			if (event.key === "Enter") {
				if (activeIndex >= 0 && options[activeIndex]) {
					const option = options[activeIndex];
					selectItem({ id: option.dataset.id, name: option.dataset.name });
				}
				return;
			}

			if (event.key === "Escape") {
				event.preventDefault();
				closePanel();
			}
		});

		moreButton.addEventListener("click", () => {
			if (!items.length) {
				return;
			}

			isExpanded = !isExpanded;
			renderResults(items);
			openPanel();
		});

		document.addEventListener("click", (event) => {
			if (!wrapper.contains(event.target)) {
				closePanel();
			}
		});
	};

	const DATETIME_SELECTOR = "[data-datetime-control]";

	const initDateTimeControl = (container) => {
		if (container.dataset.dateTimeReady === "true") {
			return;
		}

		container.dataset.dateTimeReady = "true";
		const hidden = container.querySelector("[data-datetime-hidden]");
		const display = container.querySelector("[data-datetime-display]");
		const toggle = container.querySelector("[data-datetime-toggle]");
		const popover = container.querySelector("[data-datetime-popover]");
		const yearSelect = container.querySelector("[data-datetime-year]");
		const monthSelect = container.querySelector("[data-datetime-month]");
		const daySelect = container.querySelector("[data-datetime-day]");
		const hourSelect = container.querySelector("[data-datetime-hour]");
		const minuteSelect = container.querySelector("[data-datetime-minute]");
		const nowBtn = container.querySelector("[data-datetime-now]");
		const clearBtn = container.querySelector("[data-datetime-clear]");
		const applyBtn = container.querySelector("[data-datetime-apply]");

		if (!hidden || !display || !toggle || !popover || !yearSelect || !monthSelect || !daySelect || !hourSelect || !minuteSelect || !nowBtn || !clearBtn || !applyBtn) {
			return;
		}

		const browserLocale = (navigator.languages && navigator.languages.length ? navigator.languages[0] : navigator.language) || "en-US";
		const locale = browserLocale.toLowerCase().startsWith("hr") ? "hr-HR" : "en-US";
		const monthFormatter = new Intl.DateTimeFormat(locale, { month: "long" });
		const displayFormatter = new Intl.DateTimeFormat(locale, {
			year: "numeric",
			month: "2-digit",
			day: "2-digit",
			hour: "2-digit",
			minute: "2-digit"
		});

		const parseIsoLocal = (value) => {
			if (!value) {
				return null;
			}

			const normalized = value.trim().replace(" ", "T");
			const parsed = new Date(normalized);
			if (Number.isNaN(parsed.getTime())) {
				return null;
			}

			return parsed;
		};

		const toIsoLocal = (date) => {
			const pad = (n) => String(n).padStart(2, "0");
			return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:00`;
		};

		const setDisplay = (date) => {
			display.value = date ? displayFormatter.format(date) : "";
		};

		const buildYearOptions = (centerYear) => {
			yearSelect.innerHTML = "";
			for (let y = centerYear - 20; y <= centerYear + 20; y++) {
				const option = document.createElement("option");
				option.value = String(y);
				option.textContent = String(y);
				yearSelect.appendChild(option);
			}
		};

		const buildMonthOptions = () => {
			monthSelect.innerHTML = "";
			for (let m = 0; m < 12; m++) {
				const option = document.createElement("option");
				option.value = String(m + 1);
				option.textContent = monthFormatter.format(new Date(2024, m, 1));
				monthSelect.appendChild(option);
			}
		};

		const buildHourOptions = () => {
			hourSelect.innerHTML = "";
			for (let h = 0; h < 24; h++) {
				const option = document.createElement("option");
				option.value = String(h);
				option.textContent = String(h).padStart(2, "0");
				hourSelect.appendChild(option);
			}
		};

		const buildMinuteOptions = () => {
			minuteSelect.innerHTML = "";
			for (let m = 0; m < 60; m += 1) {
				const option = document.createElement("option");
				option.value = String(m);
				option.textContent = String(m).padStart(2, "0");
				minuteSelect.appendChild(option);
			}
		};

		const rebuildDayOptions = () => {
			const y = Number.parseInt(yearSelect.value, 10);
			const m = Number.parseInt(monthSelect.value, 10);
			const current = Number.parseInt(daySelect.value || "1", 10);
			const daysInMonth = new Date(y, m, 0).getDate();

			daySelect.innerHTML = "";
			for (let d = 1; d <= daysInMonth; d++) {
				const option = document.createElement("option");
				option.value = String(d);
				option.textContent = String(d).padStart(2, "0");
				daySelect.appendChild(option);
			}

			daySelect.value = String(Math.min(current, daysInMonth));
		};

		const setSelectsFromDate = (date) => {
			buildYearOptions(date.getFullYear());
			yearSelect.value = String(date.getFullYear());
			monthSelect.value = String(date.getMonth() + 1);
			rebuildDayOptions();
			daySelect.value = String(date.getDate());
			hourSelect.value = String(date.getHours());
			minuteSelect.value = String(date.getMinutes());
		};

		const getDateFromSelects = () => {
			const y = Number.parseInt(yearSelect.value, 10);
			const m = Number.parseInt(monthSelect.value, 10);
			const d = Number.parseInt(daySelect.value, 10);
			const h = Number.parseInt(hourSelect.value, 10);
			const min = Number.parseInt(minuteSelect.value, 10);
			return new Date(y, m - 1, d, h, min, 0, 0);
		};

		const openPopover = () => {
			popover.classList.remove("is-hidden");
		};

		const closePopover = () => {
			popover.classList.add("is-hidden");
		};

		const initial = parseIsoLocal(hidden.value) || new Date();
		buildMonthOptions();
		buildHourOptions();
		buildMinuteOptions();
		setSelectsFromDate(initial);
		setDisplay(parseIsoLocal(hidden.value));

		toggle.addEventListener("click", () => {
			const current = parseIsoLocal(hidden.value) || new Date();
			setSelectsFromDate(current);
			openPopover();
		});

		yearSelect.addEventListener("change", rebuildDayOptions);
		monthSelect.addEventListener("change", rebuildDayOptions);

		nowBtn.addEventListener("click", () => {
			setSelectsFromDate(new Date());
		});

		clearBtn.addEventListener("click", () => {
			hidden.value = "";
			setDisplay(null);
			hidden.dispatchEvent(new Event("input", { bubbles: true }));
			hidden.dispatchEvent(new Event("change", { bubbles: true }));
			closePopover();
		});

		applyBtn.addEventListener("click", () => {
			const picked = getDateFromSelects();
			hidden.value = toIsoLocal(picked);
			setDisplay(picked);
			hidden.dispatchEvent(new Event("input", { bubbles: true }));
			hidden.dispatchEvent(new Event("change", { bubbles: true }));
			closePopover();
		});

		document.addEventListener("click", (event) => {
			if (!container.contains(event.target)) {
				closePopover();
			}
		});
	};

	const RAID_ANIMATOR_SELECTOR = "[data-raid-animator]";
	const RAID_MOTION_INTERVAL = 1700;

	// ─── RAID_PATTERNS ───────────────────────────────────────────────────────────
	// Only the static layout snapshots used by applyRaidLayout().
	// Custom per-boss motion is handled inside initRaidAnimator with timers.
	const RAID_PATTERNS = {
		default: {
			phase1: [
				{
					tank:   [[42,56],[58,56]],
					healer: [[44,36],[56,36],[34,46],[66,46]],
					dps:    [[24,66],[34,70],[44,74],[56,74],[66,70],[76,66]]
				},
				{
					tank:   [[45,58],[55,58]],
					healer: [[42,34],[58,34],[32,48],[68,48]],
					dps:    [[22,68],[34,72],[46,76],[54,76],[66,72],[78,68]]
				}
			],
			phase2: [
				{
					tank:   [[50,62],[50,42]],
					healer: [[30,42],[70,42],[40,30],[60,30]],
					dps:    [[26,66],[36,58],[46,66],[54,66],[64,58],[74,66]]
				},
				{
					tank:   [[52,60],[48,40]],
					healer: [[28,40],[72,40],[38,28],[62,28]],
					dps:    [[24,64],[36,56],[46,64],[54,64],[64,56],[76,64]]
				}
			],
			phase3: [
				{
					tank:   [[46,58],[54,58]],
					healer: [[38,38],[62,38],[34,54],[66,54]],
					dps:    [[28,72],[38,68],[48,72],[52,72],[62,68],[72,72]]
				},
				{
					tank:   [[44,60],[56,60]],
					healer: [[36,40],[64,40],[32,56],[68,56]],
					dps:    [[26,74],[38,70],[48,74],[52,74],[62,70],[74,74]]
				}
			]
		},

		// ── Lord Marrowgar ──────────────────────────────────────────────────────
		lordmarrowgar: {
			phase1: [
				{
					tank:   [[50,44],[52,44]],
					healer: [[50,64],[50,72],[46,68],[54,68]],
					dps:    [[50,60],[48,70],[52,70],[44,74],[56,74],[50,78]]
				},
				{
					tank:   [[50,42],[52,42]],
					healer: [[50,66],[50,74],[46,70],[54,70]],
					dps:    [[50,62],[46,72],[54,72],[40,76],[60,76],[50,80]]
				}
			],
			phase2: [
				{
					tank:   [[30,56],[70,56]],
					healer: [[46,54],[54,54],[42,60],[58,60]],
					dps:    [[40,62],[48,64],[52,64],[60,62],[46,70],[54,70]]
				}
			]
		},

		// ── Lady Deathwhisper ───────────────────────────────────────────────────
		// Base positions only; custom motion handled in initRaidAnimator
		ladydeathwhisper: {
			phase1: [
				{
					// tanks on sides, dps+heal center
					tank:   [[18,50],[82,50]],
					healer: [[44,50],[56,50],[44,58],[56,58]],
					dps:    [[50,44],[44,52],[56,52],[44,60],[56,60],[50,66]]
				}
			],
			phase2: [
				{
					// tanks+dps circling, healers center – 8 keyframes around a circle
					tank:   [[50,28],[72,64]],
					healer: [[46,50],[54,50],[46,56],[54,56]],
					dps:    [[72,36],[82,50],[72,64],[50,72],[28,64],[18,50]]
				},
				{
					tank:   [[28,64],[50,72]],
					healer: [[46,50],[54,50],[46,56],[54,56]],
					dps:    [[50,28],[72,36],[82,50],[72,64],[50,72],[28,64]]
				}
			]
		},

		// ── Gunship Battle ──────────────────────────────────────────────────────
		gunshipbattle: {
			phase1: [
				{
					// 1 tank far left, everyone else right side
					tank:   [[12,50],[72,50]],
					healer: [[62,36],[78,36],[62,64],[78,64]],
					dps:    [[68,44],[80,44],[68,50],[80,50],[68,58],[80,58]]
				}
			]
		},

		// ── Deathbringer Saurfang ───────────────────────────────────────────────
		deathbringersaurfang: {
			phase1: [
				{
					tank:   [[48,52],[52,52]],
					healer: [[28,36],[72,36],[18,55],[82,55]],
					dps:    [[36,28],[64,28],[82,42],[82,68],[64,78],[36,78]]
				}
			]
		},

		// ── Festergut ───────────────────────────────────────────────────────────
		// Base: tanks+dps center. Every 10 s dps moves to healer ring (handled in animator)
		festergut: {
			phase1: [
				{
					tank:   [[48,52],[52,52]],
					healer: [[28,36],[72,36],[18,55],[82,55]],
					dps:    [[44,50],[56,50],[44,56],[56,56],[48,62],[52,62]]
				}
			]
		},

		// ── Rotface ─────────────────────────────────────────────────────────────
		// 1 tank center, 1 tank circles, dps center, healers spread
		rotface: {
			phase1: [
				{
					tank:   [[50,52],[82,50]],
					healer: [[28,36],[72,36],[18,55],[82,68]],
					dps:    [[46,50],[54,50],[46,56],[56,56],[48,62],[52,44]]
				}
			]
		},

		// ── Professor Putricide ──────────────────────────────────────────────────
		professorputricide: {
			phase1: [
				{
					tank:   [[48,50],[52,50]],
					healer: [[44,46],[56,46],[44,54],[56,54]],
					dps:    [[46,52],[54,52],[46,58],[54,58],[48,44],[52,44]]
				}
			],
			phase2: [
				{
					// tanks left, dps+heal right
					tank:   [[22,44],[22,58]],
					healer: [[68,36],[78,36],[68,58],[78,58]],
					dps:    [[62,44],[74,44],[62,52],[74,52],[62,60],[74,60]]
				}
			],
			phase3: [
				{
					// everyone circling – 2 keyframes; animator interpolates
					tank:   [[50,28],[82,50]],
					healer: [[72,72],[28,72]],
					dps:    [[18,50],[28,28],[72,28],[82,72],[50,82],[38,60]]
				},
				{
					tank:   [[82,50],[50,72]],
					healer: [[28,28],[72,28]],
					dps:    [[50,28],[72,72],[28,72],[18,50],[38,28],[62,60]]
				}
			]
		},

		// ── Blood Prince Council ────────────────────────────────────────────────
		bloodprincecouncil: {
			phase1: [
				{
					// tanks each side, dps near left tank, healers spread
					tank:   [[18,50],[82,50]],
					healer: [[44,30],[56,30],[44,70],[56,70]],
					dps:    [[24,40],[24,50],[24,60],[32,40],[32,52],[32,62]]
				}
			],
			phase2: [
				{
					// dps moves to right tank side
					tank:   [[18,50],[82,50]],
					healer: [[44,30],[56,30],[44,70],[56,70]],
					dps:    [[76,40],[76,50],[76,60],[68,40],[68,52],[68,62]]
				}
			]
		},

		// ── Blood-Queen Lana'thel ───────────────────────────────────────────────
		bloodqueenlanathel: {
			phase1: [
				{
					tank:   [[46,52],[54,52]],
					healer: [[28,36],[72,36],[18,55],[82,55]],
					dps:    [[36,28],[64,28],[82,42],[82,68],[64,78],[36,78]]
				}
			]
		},

		// ── Valithria Dreamwalker ───────────────────────────────────────────────
		valithria: {
			phase1: [
				{
					// healers center, tanks sides, dps moving L→R (2 keyframes)
					tank:   [[12,50],[88,50]],
					healer: [[44,48],[56,48],[44,56],[56,56]],
					dps:    [[18,40],[18,55],[18,68],[82,40],[82,55],[82,68]]
				},
				{
					tank:   [[12,50],[88,50]],
					healer: [[44,48],[56,48],[44,56],[56,56]],
					dps:    [[82,40],[82,55],[82,68],[18,40],[18,55],[18,68]]
				}
			]
		},

		// ── Sindragosa ──────────────────────────────────────────────────────────
		sindragosa: {
			phase1: [
				{
					// 1 tank far left, everyone else center
					tank:   [[12,50],[50,52]],
					healer: [[44,46],[56,46],[44,56],[56,56]],
					dps:    [[46,50],[54,50],[46,58],[54,58],[48,42],[52,42]]
				}
			],
			phase2: [
				{
					// 1 tank far left, others center; healers spread bottom
					tank:   [[12,50],[50,48]],
					healer: [[28,78],[44,82],[56,82],[72,78]],
					dps:    [[44,46],[56,46],[44,54],[56,54],[48,50],[52,58]]
				}
			]
		},

		// ── The Lich King ────────────────────────────────────────────────────────
		lichking: {
			phase1: [
				{
					// everyone center
					tank:   [[46,50],[54,50]],
					healer: [[44,46],[56,46],[44,56],[56,56]],
					dps:    [[46,52],[54,52],[46,58],[54,58],[48,44],[52,44]]
				}
			],
			phase2: [
				{
					// everyone bottom; tanks sides, dps+heal between
					tank:   [[18,78],[82,78]],
					healer: [[44,74],[56,74],[44,82],[56,82]],
					dps:    [[30,72],[42,76],[50,80],[58,76],[70,72],[50,84]]
				}
			],
			phase3: [
				{
					// everyone center; animator ejects 1 dps every 10 s
					tank:   [[46,50],[54,50]],
					healer: [[44,46],[56,46],[44,56],[56,56]],
					dps:    [[46,52],[54,52],[46,58],[54,58],[48,44],[52,44]]
				}
			]
		},

		// ── Halion ────────────────────────────────────────────────────────────────
		halion: {
			phase1: [
				{
					tank:   [[36,52],[40,62]],
					healer: [[48,38],[62,38],[52,54],[66,54]],
					dps:    [[28,68],[40,74],[52,78],[60,76],[70,70],[78,62]]
				},
				{
					tank:   [[34,48],[42,60]],
					healer: [[50,34],[64,34],[54,50],[68,50]],
					dps:    [[24,66],[38,72],[50,78],[60,76],[72,70],[80,60]]
				}
			],
			phase2: [
				{
					tank:   [[30,56],[70,56]],
					healer: [[22,40],[78,40],[26,70],[74,70]],
					dps:    [[18,60],[32,76],[44,80],[56,80],[68,76],[82,60]]
				},
				{
					tank:   [[34,50],[66,50]],
					healer: [[26,34],[74,34],[30,66],[70,66]],
					dps:    [[20,56],[34,72],[46,78],[54,78],[66,72],[80,56]]
				}
			],
			phase3: [
				{
					tank:   [[46,54],[54,54]],
					healer: [[38,40],[62,40],[36,60],[64,60]],
					dps:    [[28,70],[40,74],[50,78],[60,74],[72,70],[80,64]]
				},
				{
					tank:   [[44,56],[56,56]],
					healer: [[40,44],[60,44],[38,62],[62,62]],
					dps:    [[30,72],[42,76],[50,80],[58,76],[70,72],[78,66]]
				}
			]
		},

		// ── Anub'arak ─────────────────────────────────────────────────────────────
		anubarak: {
			phase1: [
				{
					tank:   [[50,50],[50,64]],
					healer: [[38,34],[62,34],[34,50],[66,50]],
					dps:    [[26,64],[38,70],[46,74],[54,74],[62,70],[74,64]]
				},
				{
					tank:   [[46,52],[54,66]],
					healer: [[34,32],[66,32],[30,52],[70,52]],
					dps:    [[22,62],[34,68],[46,72],[54,72],[66,68],[78,62]]
				}
			],
			phase2: [
				{
					tank:   [[30,58],[70,58]],
					healer: [[20,30],[80,30],[20,70],[80,70]],
					dps:    [[14,52],[28,76],[44,82],[56,82],[72,76],[86,52]]
				},
				{
					tank:   [[38,62],[62,62]],
					healer: [[26,40],[74,40],[30,76],[70,76]],
					dps:    [[18,56],[34,70],[46,78],[54,78],[66,70],[82,56]]
				}
			],
			phase3: [
				{
					tank:   [[50,52],[50,62]],
					healer: [[44,40],[56,40],[40,54],[60,54]],
					dps:    [[36,64],[44,70],[50,74],[56,70],[64,64],[72,60]]
				},
				{
					tank:   [[48,54],[52,64]],
					healer: [[42,42],[58,42],[38,56],[62,56]],
					dps:    [[34,66],[42,72],[50,76],[58,72],[66,66],[74,62]]
				}
			]
		}
	};

	const normalizeBossKey = (bossName) => bossName.toLowerCase().replace(/[^a-z]/g, "");

	const getRaidPattern = (bossName) => {
		const key = normalizeBossKey(bossName || "");
		const baseKeys = Object.keys(RAID_PATTERNS || {});
		for (let i = 0; i < baseKeys.length; i++) {
			const k = baseKeys[i];
			if (!k) continue;
			if (key.includes(k)) {
				return { key: k, pattern: RAID_PATTERNS[k] };
			}
		}
		return { key: key || "default", pattern: RAID_PATTERNS.default };
	};

	const applyRaidLayout = (container, layout) => {
		if (!layout) return;
		Object.keys(layout).forEach((role) => {
			const positions = layout[role];
			const markers = container.querySelectorAll(`.raid-marker[data-role='${role}']`);
			markers.forEach((marker, index) => {
				const position = positions[index % positions.length];
				marker.style.setProperty("--x", `${position[0]}%`);
				marker.style.setProperty("--y", `${position[1]}%`);
			});
		});
	};

	// ─── Circle-walk helper ───────────────────────────────────────────────────────
	// Returns [x, y] percentage for a point on a circle given angle in radians.
	const circlePos = (cx, cy, rx, ry, angle) => [
		cx + rx * Math.cos(angle),
		cy + ry * Math.sin(angle)
	];

	// ─── initRaidAnimator ─────────────────────────────────────────────────────────
	const initRaidAnimator = (container) => {
		if (container.dataset.raidAnimatorReady === "true") return;
		container.dataset.raidAnimatorReady = "true";

		const arena        = container.querySelector("[data-raid-arena]");
		const buttons      = container.querySelectorAll(".phase-button");
		const bossName     = container.dataset.boss || "";
		const { key: bossKey, pattern } = getRaidPattern(bossName);

		let motionTimer  = null;
		let motionIndex  = 0;
		let teleTimers   = [];

		// ── telegraph helpers ──────────────────────────────────────────────────
		const ensureTelegraphLayer = () => {
			let layer = arena.querySelector(".raid-telegraphs");
			if (!layer) {
				layer = document.createElement("div");
				layer.className = "raid-telegraphs";
				arena.appendChild(layer);
			}
			return layer;
		};

		const spawnAoE = (x, y, r = 12, ttl = 2200, extraStyle = "") => {
			const layer = ensureTelegraphLayer();
			const el    = document.createElement("div");
			el.className = "aoe-ring";
			// Set both CSS variables (for existing CSS) and explicit geometry (failsafe).
			// left/top are the CENTER of the circle; translate(-50%,-50%) centres it.
			el.style.cssText = [
				`--x:${x}%`,
				`--y:${y}%`,
				`--r:${r}%`,
				`position:absolute`,
				`left:${x}%`,
				`top:${y}%`,
				`width:${r * 2}%`,
				`height:${r * 2}%`,
				`transform:translate(-50%,-50%)`,
				`pointer-events:none`,
				`box-sizing:border-box`,
				extraStyle
			].filter(Boolean).join(";");
			layer.appendChild(el);
			setTimeout(() => el.remove(), ttl);
		};

		const spawnLine = (angle = 0, ttl = 1800) => {
			const layer = ensureTelegraphLayer();
			const el    = document.createElement("div");
			el.className = "line-telegraph";
			el.style.setProperty("--deg", `${angle}deg`);
			layer.appendChild(el);
			setTimeout(() => el.remove(), ttl);
		};

		const spawnAdd = (x, y, ttl = 5000) => {
			const layer = ensureTelegraphLayer();
			const el    = document.createElement("div");
			el.className = "add-marker";
			el.style.setProperty("--x", `${x}%`);
			el.style.setProperty("--y", `${y}%`);
			layer.appendChild(el);
			setTimeout(() => el.remove(), ttl);
		};

		const clearTelegraphs = () => {
			teleTimers.forEach((t) => clearInterval(t));
			teleTimers = [];
			const layer = arena.querySelector(".raid-telegraphs");
			if (layer) layer.innerHTML = "";
		};

		// ── arena class flags ──────────────────────────────────────────────────
		if (arena) {
			arena.classList.toggle("is-frozen",     bossKey === "lichking");
			arena.classList.toggle("is-bone-storm",  bossKey.includes("lordmarrowgar"));
			arena.classList.toggle("is-mana-barrier",bossKey.includes("ladydeathwhisper"));
			arena.classList.toggle("is-gunship",     bossKey.includes("gunship"));
			arena.classList.toggle("is-bloodbeast",  bossKey.includes("deathbringer"));
			arena.classList.toggle("is-gas",         bossKey.includes("festergut"));
			arena.classList.toggle("is-ooze",        bossKey.includes("rotface"));
			arena.classList.toggle("is-putricide",   bossKey.includes("professor") || bossKey.includes("putricide"));
			arena.classList.toggle("is-twin-prince",  bossKey.includes("bloodprince"));
			arena.classList.toggle("is-bloodqueen",  bossKey.includes("bloodqueen"));
			arena.classList.toggle("is-valithria",   bossKey.includes("valithria"));
			arena.classList.toggle("is-sindragosa",  bossKey.includes("sindragosa"));
		}

		// ── setActive: called when a phase button is clicked ───────────────────
		const setActive = (phase) => {
			buttons.forEach((btn) =>
				btn.classList.toggle("is-active", btn.dataset.phase === phase)
			);

			// hide phase3 button for lordmarrowgar (only phase1 + phase2)
			if (bossKey.includes("lordmarrowgar")) {
				buttons.forEach((b) => {
					if (b.dataset.phase === "phase3") b.style.display = "none";
				});
			}

			// clear all running timers
			if (motionTimer) { clearInterval(motionTimer); motionTimer = null; }
			clearTelegraphs();
			motionIndex = 0;

			const layouts = pattern[phase] || pattern.phase1 || [];
			applyRaidLayout(container, layouts[0]);

			// ── standard layout oscillation (used when no custom motion below) ──
			const startDefaultMotion = () => {
				if (layouts.length > 1) {
					motionTimer = setInterval(() => {
						motionIndex = (motionIndex + 1) % layouts.length;
						applyRaidLayout(container, layouts[motionIndex]);
					}, RAID_MOTION_INTERVAL);
				}
			};

			// ═══════════════════════════════════════════════════════════════════
			// BOSS-SPECIFIC MOTION LOGIC
			// ═══════════════════════════════════════════════════════════════════

			// ── Lord Marrowgar ──────────────────────────────────────────────────
			if (bossKey.includes("lordmarrowgar")) {
				startDefaultMotion();
				teleTimers.push(setInterval(() => spawnLine(Math.random() * 360), 3000));
				teleTimers.push(setInterval(() => spawnAoE(50, 50, 18, 2600), 9000));
			}

			// ── Lady Deathwhisper ───────────────────────────────────────────────
			else if (bossKey.includes("ladydeathwhisper")) {
				if (phase === "phase1") {
					// Base layout: tanks sides, heal+dps center
					applyRaidLayout(container, layouts[0]);

					// Every 10 s: dps moves to tank positions for 3 s then returns
					teleTimers.push(setInterval(() => {
						const current = layouts[0];
						const swapped = JSON.parse(JSON.stringify(current));
						swapped.dps = current.tank.slice();
						applyRaidLayout(container, swapped);
						setTimeout(() => applyRaidLayout(container, current), 3000);
					}, 10000));

					// Every 15 s: green AoE spawns on a random center player.
					// Players whose saved position is within the circle radius nudge
					// outward (but stay within the center cluster), then return.
					const AOE_RADIUS = 8;   // % – roughly 2 player widths
					const AOE_TTL    = 5000; // ms – how long the circle lasts
					const NUDGE_DIST = 10;   // % – how far affected players step back

					teleTimers.push(setInterval(() => {
						// Collect all center markers (healers + dps, not tanks)
						const centerMarkers = [
							...container.querySelectorAll(".raid-marker[data-role='healer']"),
							...container.querySelectorAll(".raid-marker[data-role='dps']")
						];
						if (!centerMarkers.length) return;

						// Pick a random one as the target
						const target = centerMarkers[Math.floor(Math.random() * centerMarkers.length)];
						const tx = parseFloat(target.style.getPropertyValue("--x")) || 50;
						const ty = parseFloat(target.style.getPropertyValue("--y")) || 52;

						// Spawn the AoE circle on that player – no fade animation so it stays visible for full TTL
						spawnAoE(tx, ty, AOE_RADIUS, AOE_TTL, "animation:none;opacity:1;border:3px solid rgba(0,200,80,0.9);background:rgba(0,200,80,0.15)");

						// Find all center markers within the circle and nudge them outward
						centerMarkers.forEach(m => {
							const mx = parseFloat(m.style.getPropertyValue("--x")) || 50;
							const my = parseFloat(m.style.getPropertyValue("--y")) || 52;
							const dx = mx - tx;
							const dy = my - ty;
							const dist = Math.sqrt(dx * dx + dy * dy);

							if (dist < AOE_RADIUS + 2) { // +2 so edge-players also move
								// Direction away from circle center; use pure outward if exactly on center
								const nx = dist > 0.1 ? dx / dist : (Math.random() - 0.5);
								const ny = dist > 0.1 ? dy / dist : (Math.random() - 0.5);

								// New position: nudge outward, clamped to stay roughly in center area
								const newX = Math.min(70, Math.max(30, mx + nx * NUDGE_DIST));
								const newY = Math.min(70, Math.max(30, my + ny * NUDGE_DIST));

								m.style.setProperty("--x", `${newX}%`);
								m.style.setProperty("--y", `${newY}%`);

								// Return to original position when AoE expires
								setTimeout(() => {
									m.style.setProperty("--x", `${mx}%`);
									m.style.setProperty("--y", `${my}%`);
								}, AOE_TTL);
							}
						});
					}, 15000));

					// Adds on sides
					teleTimers.push(setInterval(() => {
						spawnAdd(20 + Math.random() * 10, 40 + Math.random() * 20);
						spawnAdd(70 + Math.random() * 10, 40 + Math.random() * 20);
					}, 12000));
				}

				if (phase === "phase2") {
					// Tanks+dps orbit the center, healers stay put
					// We store the full circle angle and step it each tick
					let angle = 0;
					const cx = 50, cy = 50, rx = 32, ry = 28;

					// Place healers once from base layout
					const healerPositions = [[46,50],[54,50],[46,56],[54,56]];
					const healMarkers = container.querySelectorAll(".raid-marker[data-role='healer']");
					healMarkers.forEach((m, i) => {
						const p = healerPositions[i % healerPositions.length];
						m.style.setProperty("--x", `${p[0]}%`);
						m.style.setProperty("--y", `${p[1]}%`);
					});

					// 8 moving players (2 tanks + 6 dps) spread evenly around circle
					const movingRoles = ["tank","tank","dps","dps","dps","dps","dps","dps"];
					const movingMarkers = [
						...container.querySelectorAll(".raid-marker[data-role='tank']"),
						...container.querySelectorAll(".raid-marker[data-role='dps']")
					];

					motionTimer = setInterval(() => {
						angle += (2 * Math.PI) / (8 * 6); // full rotation in ~6 intervals
						movingMarkers.forEach((m, i) => {
							const a = angle + (i * 2 * Math.PI) / movingMarkers.length;
							const [x, y] = circlePos(cx, cy, rx, ry, a);
							m.style.setProperty("--x", `${x}%`);
							m.style.setProperty("--y", `${y}%`);
						});
					}, RAID_MOTION_INTERVAL);

					// Vengeful Shade adds
					teleTimers.push(setInterval(() =>
						spawnAdd(30 + Math.random() * 40, 30 + Math.random() * 40), 8000));
				}
			}

			// ── Gunship Battle ──────────────────────────────────────────────────
			else if (bossKey.includes("gunship")) {
				// Static – just apply the single layout, no special motion
				applyRaidLayout(container, layouts[0]);
			}

			// ── Deathbringer Saurfang ────────────────────────────────────────────
			else if (bossKey.includes("deathbringer")) {
				applyRaidLayout(container, layouts[0]);
				// Blood Beasts spawn ring every 35 s
				teleTimers.push(setInterval(() => {
					for (let i = 0; i < 5; i++) {
						spawnAdd(30 + i * 9, 44 + (i % 2 ? 18 : 0), 7000);
					}
				}, 35000));
			}

			// ── Festergut ───────────────────────────────────────────────────────
			else if (bossKey.includes("festergut")) {
				// Healers spread around the ring, tanks+dps center
				const centerLayout = layouts[0];

				// Healer ring positions (spread around middle distance)
				const healerRing = [[28,36],[72,36],[18,55],[82,55]];
				// DPS center positions
				const dpsCenter  = [[44,50],[56,50],[44,56],[56,56],[48,62],[52,62]];
				// DPS outer positions (matching healer distance)
				const dpsOuter   = [[36,28],[64,28],[82,42],[82,68],[64,78],[36,78]];

				applyRaidLayout(container, centerLayout);

				// Every 10 s: dps moves out to the ring, then back after 4 s
				teleTimers.push(setInterval(() => {
					// move dps out
					const dpsMarkers = container.querySelectorAll(".raid-marker[data-role='dps']");
					dpsMarkers.forEach((m, i) => {
						const p = dpsOuter[i % dpsOuter.length];
						m.style.setProperty("--x", `${p[0]}%`);
						m.style.setProperty("--y", `${p[1]}%`);
					});
					// return after 4 s
					setTimeout(() => {
						dpsMarkers.forEach((m, i) => {
							const p = dpsCenter[i % dpsCenter.length];
							m.style.setProperty("--x", `${p[0]}%`);
							m.style.setProperty("--y", `${p[1]}%`);
						});
					}, 4000);
				}, 10000));

				// Gas Spore AoE
				teleTimers.push(setInterval(() => spawnAoE(50, 50, 10, 2800), 8000));
			}

			// ── Rotface ─────────────────────────────────────────────────────────
			else if (bossKey.includes("rotface")) {
				applyRaidLayout(container, layouts[0]);

				// Tank 2 circles the arena continuously
				let circleAngle = 0;
				const tank2 = container.querySelectorAll(".raid-marker[data-role='tank']")[1];
				motionTimer = setInterval(() => {
					circleAngle += (2 * Math.PI) / 14; // ~14 steps per rotation
					if (tank2) {
						const [x, y] = circlePos(50, 52, 38, 32, circleAngle);
						tank2.style.setProperty("--x", `${x}%`);
						tank2.style.setProperty("--y", `${y}%`);
					}
				}, RAID_MOTION_INTERVAL);

				// Every 5 s: 1 dps runs to the circling tank then returns
				const dpsMarkers = Array.from(container.querySelectorAll(".raid-marker[data-role='dps']"));
				let dpsRunnerIdx = 0;
				teleTimers.push(setInterval(() => {
					const runner = dpsMarkers[dpsRunnerIdx % dpsMarkers.length];
					dpsRunnerIdx++;
					if (!runner || !tank2) return;

					const tx = parseFloat(tank2.style.getPropertyValue("--x"));
					const ty = parseFloat(tank2.style.getPropertyValue("--y"));
					// save home position
					const hx = parseFloat(runner.style.getPropertyValue("--x")) || 50;
					const hy = parseFloat(runner.style.getPropertyValue("--y")) || 55;

					runner.style.setProperty("--x", `${tx}%`);
					runner.style.setProperty("--y", `${ty}%`);
					setTimeout(() => {
						runner.style.setProperty("--x", `${hx}%`);
						runner.style.setProperty("--y", `${hy}%`);
					}, 2200);
				}, 5000));

				// Ooze flood quarter
				teleTimers.push(setInterval(() => {
					const q = Math.floor(Math.random() * 4);
					const coords = [[20,20],[80,20],[20,80],[80,80]][q];
					spawnAoE(coords[0], coords[1], 26, 3500);
				}, 5000));
			}

			// ── Professor Putricide ──────────────────────────────────────────────
			else if (bossKey.includes("professor") || bossKey.includes("putricide")) {
				if (phase === "phase1") {
					applyRaidLayout(container, layouts[0]);
					teleTimers.push(setInterval(() => {
						spawnAoE(30 + Math.random() * 40, 30 + Math.random() * 40, 10, 3200);
						spawnAdd(20 + Math.random() * 60, 20 + Math.random() * 60, 4800);
					}, 7000));
				}

				if (phase === "phase2") {
					startDefaultMotion();
					teleTimers.push(setInterval(() =>
						spawnAoE(35 + Math.random() * 30, 40 + Math.random() * 30, 11, 3000), 7000));
				}

				if (phase === "phase3") {
					// Everyone orbits the center
					let angle = 0;
					const allMarkers = [
						...container.querySelectorAll(".raid-marker[data-role='tank']"),
						...container.querySelectorAll(".raid-marker[data-role='healer']"),
						...container.querySelectorAll(".raid-marker[data-role='dps']")
					];
					motionTimer = setInterval(() => {
						angle += (2 * Math.PI) / (allMarkers.length * 5);
						allMarkers.forEach((m, i) => {
							const a = angle + (i * 2 * Math.PI) / allMarkers.length;
							const [x, y] = circlePos(50, 52, 34, 28, a);
							m.style.setProperty("--x", `${x}%`);
							m.style.setProperty("--y", `${y}%`);
						});
					}, RAID_MOTION_INTERVAL);
				}
			}

			// ── Blood Prince Council ─────────────────────────────────────────────
			else if (bossKey.includes("bloodprince")) {
				applyRaidLayout(container, layouts[0]);
				// phase2 just re-applies the second layout
				if (phase === "phase2" && layouts[0]) {
					applyRaidLayout(container, layouts[0]);
				}
			}

			// ── Blood-Queen Lana'thel ────────────────────────────────────────────
			else if (bossKey.includes("bloodqueen")) {
				applyRaidLayout(container, layouts[0]);

				// Collect non-tank markers (healers + dps)
				const nonTankMarkers = [
					...container.querySelectorAll(".raid-marker[data-role='healer']"),
					...container.querySelectorAll(".raid-marker[data-role='dps']")
				];

				// Every 5 s: pick 2 random non-tank players, turn them "red" visually,
				// animate them toward each other, then restore after ~2 s
				teleTimers.push(setInterval(() => {
					if (nonTankMarkers.length < 2) return;

					// pick 2 distinct random indices
					const i1 = Math.floor(Math.random() * nonTankMarkers.length);
					let   i2 = Math.floor(Math.random() * (nonTankMarkers.length - 1));
					if (i2 >= i1) i2++;

					const m1 = nonTankMarkers[i1];
					const m2 = nonTankMarkers[i2];

					// save original positions
					const ox1 = m1.style.getPropertyValue("--x");
					const oy1 = m1.style.getPropertyValue("--y");
					const ox2 = m2.style.getPropertyValue("--x");
					const oy2 = m2.style.getPropertyValue("--y");

					// highlight red
					m1.style.setProperty("--marker-color", "#cc2222");
					m2.style.setProperty("--marker-color", "#cc2222");

					// midpoint
					const mx = (parseFloat(ox1) + parseFloat(ox2)) / 2;
					const my = (parseFloat(oy1) + parseFloat(oy2)) / 2;

					// move toward each other (meet in middle)
					m1.style.setProperty("--x", `${mx}%`);
					m1.style.setProperty("--y", `${my}%`);
					m2.style.setProperty("--x", `${mx}%`);
					m2.style.setProperty("--y", `${my}%`);

					// after 1.5 s: restore colour and position
					setTimeout(() => {
						m1.style.removeProperty("--marker-color");
						m2.style.removeProperty("--marker-color");
						m1.style.setProperty("--x", ox1);
						m1.style.setProperty("--y", oy1);
						m2.style.setProperty("--x", ox2);
						m2.style.setProperty("--y", oy2);
					}, 1500);
				}, 5000));
			}

			// ── Valithria Dreamwalker ────────────────────────────────────────────
			else if (bossKey.includes("valithria")) {
				// Healers + tanks stay put; dps shuttles L↔R continuously
				applyRaidLayout(container, layouts[0]);

				const dpsLeft  = [[18,40],[18,55],[18,68]];
				const dpsRight = [[82,40],[82,55],[82,68]];
				const dpsMarkers = Array.from(container.querySelectorAll(".raid-marker[data-role='dps']"));

				let dpsGoingRight = true;
				motionTimer = setInterval(() => {
					const target = dpsGoingRight ? dpsRight : dpsLeft;
					dpsMarkers.forEach((m, i) => {
						const p = target[i % target.length];
						m.style.setProperty("--x", `${p[0]}%`);
						m.style.setProperty("--y", `${p[1]}%`);
					});
					dpsGoingRight = !dpsGoingRight;
				}, RAID_MOTION_INTERVAL * 2);
			}

			// ── Sindragosa ───────────────────────────────────────────────────────
			else if (bossKey.includes("sindragosa")) {
				applyRaidLayout(container, layouts[0]);

				if (phase === "phase2") {
					// Frost Bombs: 4 random AoE in quick succession every 8 s
					teleTimers.push(setInterval(() => {
						for (let i = 0; i < 4; i++) {
							setTimeout(() => {
								spawnAoE(18 + Math.random() * 64, 20 + Math.random() * 64, 8, 3000);
							}, i * 420);
						}
					}, 8000));
				}
			}

			// ── The Lich King ────────────────────────────────────────────────────
			else if (bossKey.includes("lichking")) {
				if (phase === "phase1") {
					applyRaidLayout(container, layouts[0]);
					// Necrotic Plague beacon
					teleTimers.push(setInterval(() => spawnAoE(50, 52, 6, 2000), 15000));
				}

				if (phase === "phase2") {
					applyRaidLayout(container, layouts[0]);
					// Val'kyr AoE
					teleTimers.push(setInterval(() =>
						spawnAdd(25 + Math.random() * 50, 25 + Math.random() * 50, 5000), 20000));
					// Defile warning
					teleTimers.push(setInterval(() =>
						spawnAoE(30 + Math.random() * 40, 30 + Math.random() * 40, 8, 3000), 18000));
				}

				if (phase === "phase3") {
					applyRaidLayout(container, layouts[0]);

					// Every 10 s: eject 1 dps marker outward to simulate Harvest Soul
					const dpsMarkers = Array.from(container.querySelectorAll(".raid-marker[data-role='dps']"));
					// store home positions
					const homePos = dpsMarkers.map(m => ({
						x: m.style.getPropertyValue("--x") || "50%",
						y: m.style.getPropertyValue("--y") || "50%"
					}));
					let ejectIdx = 0;

					teleTimers.push(setInterval(() => {
						const idx = ejectIdx % dpsMarkers.length;
						ejectIdx++;
						const m = dpsMarkers[idx];
						if (!m) return;

						// eject to a random edge position
						const edge = Math.floor(Math.random() * 4);
						const ex = [50, 90, 50, 10][edge];
						const ey = [10, 50, 90, 50][edge];
						m.style.setProperty("--x", `${ex}%`);
						m.style.setProperty("--y", `${ey}%`);

						// return after 4 s
						setTimeout(() => {
							m.style.setProperty("--x", homePos[idx].x);
							m.style.setProperty("--y", homePos[idx].y);
						}, 4000);
					}, 10000));

					// Vile Spirits
					teleTimers.push(setInterval(() =>
						spawnAoE(20 + Math.random() * 60, 20 + Math.random() * 60, 6, 2000), 30000));
				}
			}

			// ── All other bosses (halion, anubarak, default) ─────────────────────
			else {
				startDefaultMotion();
			}
		};

		// ── wire up phase buttons ──────────────────────────────────────────────
		buttons.forEach((button) => {
			button.addEventListener("click", () => {
				setActive(button.dataset.phase || "phase1");
			});
		});

		const defaultPhase =
			container.querySelector(".phase-button.is-active")?.dataset.phase || "phase1";
		setActive(defaultPhase);
	};

	// ─── initAll ──────────────────────────────────────────────────────────────────
	const initAll = (root) => {
		configureValidation();
		root.querySelectorAll(AUTOCOMPLETE_SELECTOR).forEach((c) => initAutocomplete(c));
		root.querySelectorAll(SUGGEST_SELECTOR).forEach((i) => initListSuggest(i));
		root.querySelectorAll(DATETIME_SELECTOR).forEach((c) => initDateTimeControl(c));
		root.querySelectorAll("[data-raid-animator]").forEach((c) => initRaidAnimator(c));
	};

	if (document.readyState === "loading") {
		document.addEventListener("DOMContentLoaded", () => initAll(document));
	} else {
		initAll(document);
	}
})();