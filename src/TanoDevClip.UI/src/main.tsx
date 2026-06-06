import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { tanoDevBridge } from "./bridge/tanoDevBridge.ts";
import "./index.css";
import App from "./App.tsx";

tanoDevBridge.initialize();

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
