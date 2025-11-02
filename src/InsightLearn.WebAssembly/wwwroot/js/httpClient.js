// Native JavaScript fetch wrapper for Blazor WASM
// This bypasses HttpClient issues with cross-origin requests

window.httpClientHelper = {
    /**
     * Make a POST request using native fetch API
     * @param {string} url - Full URL to POST to
     * @param {object} data - Data to send as JSON
     * @returns {Promise<object>} Response object with { success, status, data }
     */
    postJson: async function (url, data) {
        try {
            console.log('🔍 JS httpClient.postJson - URL:', url);
            console.log('🔍 JS httpClient.postJson - Data:', data);

            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(data)
            });

            console.log('🔍 JS httpClient.postJson - Status:', response.status);
            console.log('🔍 JS httpClient.postJson - Headers:', [...response.headers.entries()]);

            const responseData = await response.json();
            console.log('🔍 JS httpClient.postJson - Response:', responseData);

            return {
                success: response.ok,
                status: response.status,
                data: responseData
            };

        } catch (error) {
            console.error('❌ JS httpClient.postJson - Error:', error);
            return {
                success: false,
                status: 0,
                error: error.message
            };
        }
    },

    /**
     * Make a GET request using native fetch API
     * @param {string} url - Full URL to GET from
     * @returns {Promise<object>} Response object with { success, status, data }
     */
    getJson: async function (url) {
        try {
            console.log('🔍 JS httpClient.getJson - URL:', url);

            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            console.log('🔍 JS httpClient.getJson - Status:', response.status);

            const responseData = await response.json();

            return {
                success: response.ok,
                status: response.status,
                data: responseData
            };

        } catch (error) {
            console.error('❌ JS httpClient.getJson - Error:', error);
            return {
                success: false,
                status: 0,
                error: error.message
            };
        }
    }
};
