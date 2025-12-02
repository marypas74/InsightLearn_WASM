/**
 * PayPal Checkout Integration for InsightLearn
 * Handles PayPal button rendering and payment processing
 */

window.initializePayPalButton = function (amount, currency, dotNetReference) {
    console.log('[PayPal] Initializing PayPal button', { amount, currency });

    // Ensure PayPal SDK is loaded
    if (typeof paypal === 'undefined') {
        console.error('[PayPal] PayPal SDK not loaded');
        dotNetReference.invokeMethodAsync('OnPayPalError', 'PayPal SDK not loaded. Please refresh the page.');
        return;
    }

    // Clear existing button
    const container = document.getElementById('paypal-button-container');
    if (!container) {
        console.error('[PayPal] Button container not found');
        return;
    }
    container.innerHTML = '';

    // Render PayPal button
    paypal.Buttons({
        style: {
            layout: 'vertical',
            color: 'gold',
            shape: 'rect',
            label: 'paypal',
            height: 50,
            tagline: false
        },

        // Create order on PayPal
        createOrder: function (data, actions) {
            console.log('[PayPal] Creating order', { amount, currency });
            return actions.order.create({
                purchase_units: [{
                    amount: {
                        currency_code: currency.toUpperCase(),
                        value: amount
                    },
                    description: 'InsightLearn Course Purchase'
                }],
                application_context: {
                    shipping_preference: 'NO_SHIPPING', // Digital goods - no shipping
                    brand_name: 'InsightLearn',
                    locale: 'en-US',
                    landing_page: 'BILLING', // Go directly to billing page
                    user_action: 'PAY_NOW' // Show "Pay Now" button instead of "Continue"
                }
            });
        },

        // On payment approval
        onApprove: function (data, actions) {
            console.log('[PayPal] Payment approved', data);

            // Show processing spinner
            container.innerHTML = `
                <div style="text-align: center; padding: 2rem;">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Processing...</span>
                    </div>
                    <p class="mt-3 text-muted">Completing your purchase...</p>
                </div>
            `;

            // Capture the payment
            return actions.order.capture().then(function (details) {
                console.log('[PayPal] Payment captured successfully', details);

                // Call Blazor method to handle successful payment
                dotNetReference.invokeMethodAsync('OnPayPalApprove', data.orderID)
                    .catch(function (error) {
                        console.error('[PayPal] Error calling OnPayPalApprove:', error);
                        dotNetReference.invokeMethodAsync('OnPayPalError', 'Failed to process payment. Please contact support.');
                    });
            }).catch(function (error) {
                console.error('[PayPal] Error capturing payment:', error);
                dotNetReference.invokeMethodAsync('OnPayPalError', 'Payment capture failed. Please try again.');
            });
        },

        // On payment error
        onError: function (err) {
            console.error('[PayPal] Payment error:', err);
            let errorMessage = 'An error occurred during payment. Please try again.';

            if (err && err.message) {
                errorMessage = err.message;
            }

            dotNetReference.invokeMethodAsync('OnPayPalError', errorMessage);
        },

        // On payment cancel
        onCancel: function (data) {
            console.log('[PayPal] Payment cancelled by user', data);
            dotNetReference.invokeMethodAsync('OnPayPalCancel');
        }

    }).render('#paypal-button-container')
        .then(function () {
            console.log('[PayPal] Button rendered successfully');
        })
        .catch(function (error) {
            console.error('[PayPal] Error rendering button:', error);
            dotNetReference.invokeMethodAsync('OnPayPalError', 'Failed to load PayPal button. Please refresh the page.');
        });
};

/**
 * Cleanup PayPal button (call when component is disposed)
 */
window.cleanupPayPalButton = function () {
    const container = document.getElementById('paypal-button-container');
    if (container) {
        container.innerHTML = '';
        console.log('[PayPal] Button cleanup completed');
    }
};
