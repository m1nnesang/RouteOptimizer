window.driverConnectivity = {
    isOnline: function () {
        return navigator.onLine;
    },

    register: function (dotnetRef) {
        window.addEventListener("online", function () {
            dotnetRef.invokeMethodAsync("OnConnectivityChanged", true);
        });
        window.addEventListener("offline", function () {
            dotnetRef.invokeMethodAsync("OnConnectivityChanged", false);
        });
    }
};
