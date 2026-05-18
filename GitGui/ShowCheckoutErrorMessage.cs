namespace GitGui;

// Broadcast when a checkout attempt fails — OverlayView shows CheckoutErrorDialog with
// git's stderr message.
public readonly record struct ShowCheckoutErrorMessage(string Message);
