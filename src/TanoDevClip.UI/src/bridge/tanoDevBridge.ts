export type BridgeMessage = {
  type: string;
  payload?: unknown;
};

type MessageHandler = (message: BridgeMessage) => void;

declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage: (message: BridgeMessage) => void;
        addEventListener: (
          eventName: "message",
          handler: (event: { data: BridgeMessage }) => void,
        ) => void;
      };
    };
  }
}

const handlers = new Set<MessageHandler>();

export const tanoDevBridge = {
  isAvailable(): boolean {
    return Boolean(window.chrome?.webview);
  },

  send(message: BridgeMessage): void {
    if (!window.chrome?.webview) {
      console.warn("TanoDev bridge is not available.", message);
      return;
    }

    window.chrome.webview.postMessage(message);
  },

  onMessage(handler: MessageHandler): () => void {
    handlers.add(handler);

    return () => {
      handlers.delete(handler);
    };
  },

  initialize(): void {
    if (!window.chrome?.webview) {
      return;
    }

    window.chrome.webview.addEventListener("message", (event) => {
      handlers.forEach((handler) => handler(event.data));
    });
  },
};
