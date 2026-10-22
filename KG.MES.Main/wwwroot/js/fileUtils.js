window.fileUtils =
{
	triggerFileInput: function (elementId)
	{
		const el = document.getElementById(elementId);
		if (el) el.click();
	}
};

window.autoResizeTextarea = function (element)
{
	if (!element) return;

	// Сбрасываем высоту, чтобы получить реальную scrollHeight
	//element.style.height = 'auto';

	// Устанавливаем высоту по содержимому
	var newHeight = element.scrollHeight;
	if (newHeight < 100) newHeight = 100; // минимальная высота

	element.style.height = newHeight + 'px';
};

window.collapseTextarea = function (element)
{
	if (!element) return;
	element.style.height = '31px';
};