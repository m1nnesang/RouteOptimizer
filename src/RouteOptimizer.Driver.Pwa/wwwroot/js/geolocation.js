window.driverGeolocation = {
    getCurrentPosition: function (timeoutMs) {
        return new Promise(function (resolve, reject) {
            if (!navigator.geolocation) {
                reject("Geolocation is not supported on this device.");
                return;
            }

            navigator.geolocation.getCurrentPosition(
                function (position) {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    });
                },
                function (error) {
                    reject(error && error.message ? error.message : "Could not determine your location.");
                },
                {
                    enableHighAccuracy: true,
                    timeout: timeoutMs,
                    maximumAge: 0
                });
        });
    }
};
