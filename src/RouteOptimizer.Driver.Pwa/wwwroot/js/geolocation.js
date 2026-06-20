window.driverGeolocation = {
    getCurrentPosition: function (timeoutMs) {
        return new Promise(function (resolve, reject) {
            if (!navigator.geolocation) {
                reject("Геолокация не поддерживается этим устройством.");
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
                    reject(error && error.message ? error.message : "Не удалось определить координаты.");
                },
                {
                    enableHighAccuracy: true,
                    timeout: timeoutMs,
                    maximumAge: 0
                });
        });
    }
};
