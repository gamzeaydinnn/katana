// jest-dom adds custom jest matchers for asserting on DOM nodes.
// allows you to do things like:
// expect(element).toHaveTextContent(/react/i)
// learn more: https://github.com/testing-library/jest-dom
import "@testing-library/jest-dom";

// Polyfills for MSW (Mock Service Worker) in Jest/Node environment
// MSW requires TextEncoder/TextDecoder which are not available in Node.js by default
import { TextEncoder, TextDecoder } from "util";

global.TextEncoder = TextEncoder as any;
global.TextDecoder = TextDecoder as any;

// BroadcastChannel polyfill for MSW WebSocket support
class BroadcastChannelPolyfill {
  name: string;
  onmessage: ((event: MessageEvent) => void) | null = null;

  constructor(name: string) {
    this.name = name;
  }

  postMessage(message: any) {
    // Mock implementation - no-op for tests
  }

  close() {
    // Mock implementation - no-op for tests
  }

  addEventListener() {
    // Mock implementation - no-op for tests
  }

  removeEventListener() {
    // Mock implementation - no-op for tests
  }

  dispatchEvent() {
    return true;
  }
}

(global as any).BroadcastChannel = BroadcastChannelPolyfill;
