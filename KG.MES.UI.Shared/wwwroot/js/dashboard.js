window.dashboardDragDrop =
{
	init: function (dotnetHelper)
	{
		document.addEventListener('dragover', function (e)
		{
			e.preventDefault();
		});

		document.addEventListener('drop', function (e)
		{
			e.preventDefault();
		});

		//document.addEventListener('dragstart', function (e)
		//{
		//	const widget = e.target.closest('.dashboard-widget');
		//	if (widget)
		//	{
		//		widget.classList.add('dragging');
		//	}
		//});

		//document.addEventListener('dragend', function (e)
		//{
		//	const widget = e.target.closest('.dashboard-widget');
		//	if (widget)
		//	{
		//		widget.classList.remove('dragging');
		//		document.querySelectorAll('.drag-over').forEach(el => el.classList.remove('drag-over'));
		//	}
		//});

		document.addEventListener('dragstart', function (e)
		{
			const widget = e.target.closest('.dashboard-widget');
			const tab = e.target.closest('.dashboard-tab');

			if (widget)
			{
				widget.classList.add('dragging');
			}
			
			if (tab)
			{
				tab.classList.add('dragging');
			}
		});

		document.addEventListener('dragend', function (e)
		{
			document.querySelectorAll('.dragging').forEach(el => el.classList.remove('dragging'));
			document.querySelectorAll('.drag-over').forEach(el => el.classList.remove('drag-over'));
		});

		// Перехватываем dragover/drop на всём документе
		document.addEventListener('dragover', function (e)
		{
			const widget = e.target.closest('.dashboard-widget');
			if (widget)
			{
				document.querySelectorAll('.drag-over').forEach(el =>
				{
					if (el !== widget) el.classList.remove('drag-over');
				});
				widget.classList.add('drag-over');
			}
		});

		document.addEventListener('drop', function (e)
		{
			e.preventDefault();

			const targetWidget = e.target.closest('.dashboard-widget');
			const targetTabs = e.target.closest('.dashboard-tabs');
			const draggedWidget = document.querySelector('.dashboard-widget.dragging');
			const draggedTab = document.querySelector('.dashboard-tab.dragging');

			document.querySelectorAll('.drag-over').forEach(el => el.classList.remove('drag-over'));
			document.querySelectorAll('.dragging').forEach(el => el.classList.remove('dragging'));

			if (draggedWidget)
			{
				// Тащим виджет из сетки
				const draggedId = draggedWidget.dataset.widgetId;

				if (targetTabs && !targetWidget)
				{
					// Бросили на панель вкладок — сворачиваем
					console.log('Drop widget to tabs:', draggedId);
					dotnetHelper.invokeMethodAsync('OnDropToTabs', draggedId);
				}
				else if (targetWidget && targetWidget !== draggedWidget)
				{
					// Бросили на другой виджет — обмен
					const targetId = targetWidget.dataset.widgetId;
					console.log('Swap widgets:', draggedId, '->', targetId);
					dotnetHelper.invokeMethodAsync('OnWidgetDrop', draggedId, targetId);
				}
			}
			else if (draggedTab)
			{
				// Тащим вкладку
				const draggedId = draggedTab.dataset.widgetId;

				if (!targetTabs && !targetWidget)
				{
					// Бросили в основную область (не на виджет и не на табы) — разворачиваем в сетку
					console.log('Drop tab to main:', draggedId);
					dotnetHelper.invokeMethodAsync('OnDropToMain', draggedId);
				}
				else if (targetWidget && targetWidget.dataset.widgetId !== draggedId)
				{
					// Бросили на виджет — разворачиваем и меняем местами
					console.log('Drop tab to widget:', draggedId, '->', targetWidget.dataset.widgetId);
					dotnetHelper.invokeMethodAsync('OnDropTabToWidget', draggedId, targetWidget.dataset.widgetId);
				}
			}
		});

		document.querySelectorAll('.dashboard-widget').forEach(widget =>
		{
			widget.addEventListener('mousedown', function (e)
			{
				const rect = widget.getBoundingClientRect();
				const isRightEdge = e.clientX > rect.right - 10;

				if (isRightEdge)
				{
					e.preventDefault();
					e.stopPropagation();

					const startX = e.clientX;
					const startWidth = rect.width;

					function onMouseMove(me)
					{
						const newWidth = startWidth + (me.clientX - startX);
						widget.style.width = Math.max(200, newWidth) + 'px';
						widget.style.gridColumn = 'auto';
					}

					function onMouseUp()
					{
						document.removeEventListener('mousemove', onMouseMove);
						document.removeEventListener('mouseup', onMouseUp);
						// Сохранить ширину
						const widgetId = widget.dataset.widgetId;
						dotnetHelper.invokeMethodAsync('OnWidgetResize', widgetId, widget.style.width);
					}

					document.addEventListener('mousemove', onMouseMove);
					document.addEventListener('mouseup', onMouseUp);
				}
			});
		});
	},

	getDraggedId: function ()
	{
		const dragged = document.querySelector('.dashboard-widget.dragging');
		return dragged ? dragged.dataset.widgetId : '';
	}
};