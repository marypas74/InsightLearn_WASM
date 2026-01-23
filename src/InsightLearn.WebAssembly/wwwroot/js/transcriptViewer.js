/**
 * TranscriptViewer JavaScript Module
 * Fix v2.3.61: Secure JSInterop functions without using inline code execution
 * Provides safe methods for browser interactions
 */
window.TranscriptViewer = {
    /**
     * Download a file from the API with authentication
     * @param {string} downloadUrl - The API endpoint URL
     * @param {string} filename - The filename for the downloaded file
     * @param {string} authToken - JWT authentication token
     * @returns {Promise<boolean>} - Success status
     */
    downloadFile: async function(downloadUrl, filename, authToken) {
        try {
            const response = await fetch(downloadUrl, {
                method: 'GET',
                headers: {
                    'Authorization': 'Bearer ' + (authToken || '')
                }
            });

            if (!response.ok) {
                console.error('[TranscriptViewer] Download failed:', response.status, response.statusText);
                throw new Error('Download failed: ' + response.statusText);
            }

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
            return true;
        } catch (error) {
            console.error('[TranscriptViewer] Download error:', error);
            return false;
        }
    },

    /**
     * Get the browser's preferred language
     * @returns {string} - Language code (e.g., 'en-US', 'it-IT')
     */
    getBrowserLanguage: function() {
        return navigator.language || navigator.userLanguage || 'en';
    },

    /**
     * Scroll to a specific segment element in the transcript container
     * @param {number} segmentIndex - The index of the segment to scroll to
     */
    scrollToSegment: function(segmentIndex) {
        try {
            const element = document.getElementById('segment-' + segmentIndex);
            const container = document.getElementById('transcript-scroll-container');
            if (element && container) {
                const elementRect = element.getBoundingClientRect();
                const containerRect = container.getBoundingClientRect();
                if (elementRect.top < containerRect.top || elementRect.bottom > containerRect.bottom) {
                    element.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            }
        } catch (error) {
            console.error('[TranscriptViewer] Scroll error:', error);
        }
    }
};

console.log('[TranscriptViewer] JavaScript module loaded');
