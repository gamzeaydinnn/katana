



import "@testing-library/jest-dom";



import { TextEncoder, TextDecoder } from "util";

global.TextEncoder = TextEncoder as any;
global.TextDecoder = TextDecoder as any;


class BroadcastChannelPolyfill {
  name: string;
  onmessage: ((event: MessageEvent) => void) | null = null;

  constructor(name: string) {
    this.name = name;
  }

  postMessage(message: any) {
    
  }

  close() {
    
  }

  addEventListener() {
    
  }

  removeEventListener() {
    
  }

  dispatchEvent() {
    return true;
  }
}

(global as any).BroadcastChannel = BroadcastChannelPolyfill;
