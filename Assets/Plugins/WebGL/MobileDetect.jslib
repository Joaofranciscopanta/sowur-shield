mergeInto(LibraryManager.library, {
    IsMobileBrowser: function () {
        var ua = navigator.userAgent || navigator.vendor || "";
        var uaIsMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(ua);
        var hasTouchPoints = (navigator.maxTouchPoints || 0) > 0;
        return (uaIsMobile || hasTouchPoints) ? 1 : 0;
    }
});
