window.driverSlider = {
    observe: function (elementId, dotnetRef) {
        const container = document.getElementById(elementId);
        if (!container) return;

        if (container._sliderObserver) {
            container._sliderObserver.disconnect();
        }

        const observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting && e.intersectionRatio >= 0.6) {
                    const id = e.target.getAttribute("data-stop-id");
                    if (id) dotnetRef.invokeMethodAsync("OnStopFocused", id);
                }
            });
        }, { root: container, threshold: [0.6] });

        Array.prototype.forEach.call(container.children, function (c) { observer.observe(c); });
        container._sliderObserver = observer;
    },

    scrollTo: function (elementId, stopId) {
        const container = document.getElementById(elementId);
        if (!container) return;

        const child = container.querySelector('[data-stop-id="' + stopId + '"]');
        if (child) child.scrollIntoView({ behavior: "instant", inline: "center", block: "nearest" });
    },

    dispose: function (elementId) {
        const container = document.getElementById(elementId);
        if (container && container._sliderObserver) {
            container._sliderObserver.disconnect();
            container._sliderObserver = null;
        }
    }
};
