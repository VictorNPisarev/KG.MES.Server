window.fieldArrow = {
	draw: function (fromId, toId) {
		const from = document.getElementById(fromId);
		const to = document.getElementById(toId);
		if (!from || !to) return;

		const fromRect = from.getBoundingClientRect();
		const toRect = to.getBoundingClientRect();

		const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
		svg.style.position = "fixed";
		svg.style.top = "0";
		svg.style.left = "0";
		svg.style.width = "100%";
		svg.style.height = "100%";
		svg.style.pointerEvents = "none";
		svg.style.zIndex = "9999";
		document.body.appendChild(svg);

		const x1 = fromRect.right;
		const y1 = fromRect.top + fromRect.height / 2;
		const x2 = toRect.left;
		const y2 = toRect.top + toRect.height / 2;

		// Линия
		const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
		line.setAttribute("x1", x1);
		line.setAttribute("y1", y1);
		line.setAttribute("x2", x1);
		line.setAttribute("y2", y1);
		line.setAttribute("stroke", "#198754");
		line.setAttribute("stroke-width", "1");
		svg.appendChild(line);

		// Стрелка
		const arrow = document.createElementNS("http://www.w3.org/2000/svg", "polygon");
		const arrowSize = 6;
		arrow.setAttribute("fill", "#198754");
		svg.appendChild(arrow);

		const duration = 250;
		const start = performance.now();

		function updateArrow(cx, cy, tx, ty) {
			const angle = Math.atan2(ty - cy, tx - cx);
			const points = [
				[cx, cy],
				[cx - arrowSize * Math.cos(angle - 0.6), cy - arrowSize * Math.sin(angle - 0.6)],
				[cx - arrowSize * Math.cos(angle + 0.6), cy - arrowSize * Math.sin(angle + 0.6)]
			].map(p => p.join(',')).join(' ');
			arrow.setAttribute("points", points);
		}

		function animate(now) {
			const elapsed = now - start;
			const progress = Math.min(elapsed / duration, 1);

			const cx = x1 + (x2 - x1) * progress;
			const cy = y1 + (y2 - y1) * progress;
			line.setAttribute("x2", cx);
			line.setAttribute("y2", cy);
			updateArrow(cx, cy, x2, y2);

			if (progress >= 1) {
				setTimeout(() => svg.remove(), 100);
			} else {
				requestAnimationFrame(animate);
			}
		}

		updateArrow(x1, y1, x2, y2);
		requestAnimationFrame(animate);
	}
};