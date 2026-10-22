window.panelResize = {
	_observers: {}, // Храним observer'ы, чтобы можно было отписаться

	init: function (dotnetRef, panelId) {
		if (!dotnetRef) {
			console.error('panelResize.init: dotnetRef is null');
			return;
		}

		const panel = document.getElementById(panelId);
		if (!panel) {
			console.warn('panelResize.init: Panel not found:', panelId);
			return;
		}

		// Если уже есть observer для этой панели — удаляем
		if (this._observers[panelId]) {
			this._observers[panelId].disconnect();
		}

		const observer = new ResizeObserver(entries => {
			for (let entry of entries) {
				const width = entry.contentRect.width;
				// Защита: вызываем только если ширина реально изменилась значительно
				try {
					dotnetRef.invokeMethodAsync('OnPanelResized', width);
				} catch (e) {
					console.warn('panelResize: invokeMethodAsync failed', e);
					// Если circuit умер — отключаем observer
					observer.disconnect();
				}
			}
		});

		observer.observe(panel);
		this._observers[panelId] = observer;
	},

	dispose: function (panelId) {
		if (this._observers[panelId]) {
			this._observers[panelId].disconnect();
			delete this._observers[panelId];
		}
	}
};