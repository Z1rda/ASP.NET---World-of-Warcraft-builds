// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
	const AUTOCOMPLETE_SELECTOR = "[data-autocomplete-url][data-autocomplete-name]";
	const SUGGEST_SELECTOR = "input[data-suggest-url]";

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

	const initAll = (root) => {
		const containers = root.querySelectorAll(AUTOCOMPLETE_SELECTOR);
		containers.forEach((container) => initAutocomplete(container));

		const suggestInputs = root.querySelectorAll(SUGGEST_SELECTOR);
		suggestInputs.forEach((input) => initListSuggest(input));
	};

	if (document.readyState === "loading") {
		document.addEventListener("DOMContentLoaded", () => initAll(document));
	} else {
		initAll(document);
	}
})();
