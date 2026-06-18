import { useCallback, useEffect, useState } from "react";
import { devToolDefinitions } from "../constants";
import type { GuidFormat, ToolKind, ToolResult } from "../types";

type DevToolsViewProps = {
  activeTool: ToolKind;
  enabledTools: ToolKind[];
  result: ToolResult;
  onToolChange: (tool: ToolKind) => void;
  onRun: (payload: DevToolPayload) => void;
  onCopy: (content: string, kind: ToolKind) => void;
};

export type DevToolPayload = {
  tool: ToolKind;
  action: string;
  format?: GuidFormat;
  input?: string;
  amount?: number;
  length?: number;
  formatted?: boolean;
  includeUppercase?: boolean;
  includeLowercase?: boolean;
  includeNumbers?: boolean;
  includeSymbols?: boolean;
  pattern?: string;
  flags?: string;
  sample?: string;
  replacement?: string;
};

export function DevToolsView({
  activeTool,
  enabledTools,
  result,
  onToolChange,
  onRun,
  onCopy,
}: DevToolsViewProps) {
  const [guidFormat, setGuidFormat] = useState<GuidFormat>("default");
  const [formattedCpf, setFormattedCpf] = useState(true);
  const [cpfInput, setCpfInput] = useState("");
  const [formattedCnpj, setFormattedCnpj] = useState(true);
  const [cnpjInput, setCnpjInput] = useState("");
  const [loremAmount, setLoremAmount] = useState(180);
  const [stringLength, setStringLength] = useState(32);
  const [includeUppercase, setIncludeUppercase] = useState(true);
  const [includeLowercase, setIncludeLowercase] = useState(true);
  const [includeNumbers, setIncludeNumbers] = useState(true);
  const [includeSymbols, setIncludeSymbols] = useState(false);
  const [jwtInput, setJwtInput] = useState("");
  const [jsonInput, setJsonInput] = useState("");
  const [base64Input, setBase64Input] = useState("");
  const [urlInput, setUrlInput] = useState("");
  const [regexPattern, setRegexPattern] = useState("");
  const [regexFlags, setRegexFlags] = useState("i");
  const [regexSample, setRegexSample] = useState("");
  const [regexReplacement, setRegexReplacement] = useState("");
  const visibleTools = devToolDefinitions.filter((tool) =>
    enabledTools.includes(tool.id),
  );
  const isActiveToolEnabled = enabledTools.includes(activeTool);

  const buildPayload = useCallback((action: string): DevToolPayload => {
    if (activeTool === "guid") {
      return { tool: activeTool, action, format: guidFormat };
    }

    if (activeTool === "cpf") {
      return {
        tool: activeTool,
        action,
        formatted: formattedCpf,
        input: cpfInput,
      };
    }

    if (activeTool === "cnpj") {
      return {
        tool: activeTool,
        action,
        formatted: formattedCnpj,
        input: cnpjInput,
      };
    }

    if (activeTool === "lorem") {
      return { tool: activeTool, action, amount: loremAmount };
    }

    if (activeTool === "string") {
      return {
        tool: activeTool,
        action,
        length: stringLength,
        includeUppercase,
        includeLowercase,
        includeNumbers,
        includeSymbols,
      };
    }

    if (activeTool === "jwt") {
      return { tool: activeTool, action, input: jwtInput };
    }

    if (activeTool === "json") {
      return { tool: activeTool, action, input: jsonInput };
    }

    if (activeTool === "base64") {
      return { tool: activeTool, action, input: base64Input };
    }

    if (activeTool === "url") {
      return { tool: activeTool, action, input: urlInput };
    }

    return {
      tool: activeTool,
      action,
      pattern: regexPattern,
      flags: regexFlags,
      sample: regexSample,
      replacement: regexReplacement,
    };
  }, [
    activeTool,
    base64Input,
    cnpjInput,
    cpfInput,
    guidFormat,
    formattedCnpj,
    formattedCpf,
    includeLowercase,
    includeNumbers,
    includeSymbols,
    includeUppercase,
    jsonInput,
    jwtInput,
    loremAmount,
    regexFlags,
    regexPattern,
    regexReplacement,
    regexSample,
    stringLength,
    urlInput,
  ]);

  const run = useCallback((action: string) => {
    onRun(buildPayload(action));
  }, [buildPayload, onRun]);

  useEffect(() => {
    if (!isActiveToolEnabled) {
      return;
    }

    if (activeTool === "guid") {
      onRun({ tool: activeTool, action: "generate", format: guidFormat });
    }

    if (activeTool === "cpf") {
      onRun({ tool: activeTool, action: "generate", formatted: formattedCpf });
    }

    if (activeTool === "cnpj") {
      onRun({ tool: activeTool, action: "generate", formatted: formattedCnpj });
    }

    if (activeTool === "lorem") {
      onRun({ tool: activeTool, action: "generate", amount: loremAmount });
    }

    if (activeTool === "string") {
      onRun({
        tool: activeTool,
        action: "generate",
        length: stringLength,
        includeUppercase,
        includeLowercase,
        includeNumbers,
        includeSymbols,
      });
    }
  }, [
    activeTool,
    formattedCnpj,
    formattedCpf,
    guidFormat,
    includeLowercase,
    includeNumbers,
    includeSymbols,
    includeUppercase,
    isActiveToolEnabled,
    loremAmount,
    onRun,
    stringLength,
  ]);

  function handleCopy() {
    if (result.status !== "ok" || !result.value) {
      return;
    }

    onCopy(result.value, activeTool);
  }

  return (
    <section className="dev-drawer">
      <div className="drawer-title">
        <strong>./devtools</strong>
        <span>{isActiveToolEnabled ? activeTool : "disabled"}</span>
      </div>

      <div className="tool-tabs">
        {visibleTools.map((tool) => (
          <button
            key={tool.id}
            className={activeTool === tool.id ? "active" : ""}
            onClick={() => onToolChange(tool.id)}
          >
            {tool.label}
          </button>
        ))}
      </div>

      {visibleTools.length === 0 ? (
        <div className="empty-state small">
          Enable at least one tool in settings.
        </div>
      ) : (
        <>
          <div className="tool-workbench">{renderToolBody()}</div>

          <textarea
            className={`generated-output output-${result.status}`}
            readOnly
            value={isActiveToolEnabled ? result.value : ""}
            placeholder="<waiting for output>"
          />

          <div className="tool-actions">
            {renderActions()}
            <button
              disabled={
                !isActiveToolEnabled ||
                result.status !== "ok" ||
                !result.value
              }
              onClick={handleCopy}
            >
              copy
            </button>
          </div>
        </>
      )}
    </section>
  );

  function renderToolBody() {
    if (activeTool === "guid") {
      return (
        <div className="segmented-control">
          <button
            className={guidFormat === "default" ? "active" : ""}
            onClick={() => setGuidFormat("default")}
          >
            default
          </button>
          <button
            className={guidFormat === "no-hyphens" ? "active" : ""}
            onClick={() => setGuidFormat("no-hyphens")}
          >
            compact
          </button>
          <button
            className={guidFormat === "uppercase" ? "active" : ""}
            onClick={() => setGuidFormat("uppercase")}
          >
            upper
          </button>
        </div>
      );
    }

    if (activeTool === "cpf") {
      return (
        <div className="tool-options document-options">
          <label>
            <input
              type="checkbox"
              checked={formattedCpf}
              onChange={(event) => setFormattedCpf(event.target.checked)}
            />
            formatted
          </label>
          <input
            value={cpfInput}
            onChange={(event) => setCpfInput(event.target.value)}
            placeholder="CPF to validate"
          />
        </div>
      );
    }

    if (activeTool === "cnpj") {
      return (
        <div className="tool-options document-options">
          <label>
            <input
              type="checkbox"
              checked={formattedCnpj}
              onChange={(event) => setFormattedCnpj(event.target.checked)}
            />
            formatted
          </label>
          <input
            value={cnpjInput}
            onChange={(event) => setCnpjInput(event.target.value)}
            placeholder="CNPJ to validate"
          />
        </div>
      );
    }

    if (activeTool === "lorem") {
      return (
        <div className="tool-options single-number-options">
          <label>
            chars
            <input
              type="number"
              min={1}
              max={5000}
              value={loremAmount}
              onChange={(event) => setLoremAmount(Number(event.target.value))}
            />
          </label>
        </div>
      );
    }

    if (activeTool === "string") {
      return (
        <div className="tool-options string-options">
          <label>
            len
            <input
              type="number"
              min={1}
              max={512}
              value={stringLength}
              onChange={(event) => setStringLength(Number(event.target.value))}
            />
          </label>
          <label>
            <input
              type="checkbox"
              checked={includeUppercase}
              onChange={(event) => setIncludeUppercase(event.target.checked)}
            />
            A-Z
          </label>
          <label>
            <input
              type="checkbox"
              checked={includeLowercase}
              onChange={(event) => setIncludeLowercase(event.target.checked)}
            />
            a-z
          </label>
          <label>
            <input
              type="checkbox"
              checked={includeNumbers}
              onChange={(event) => setIncludeNumbers(event.target.checked)}
            />
            0-9
          </label>
          <label>
            <input
              type="checkbox"
              checked={includeSymbols}
              onChange={(event) => setIncludeSymbols(event.target.checked)}
            />
            sym
          </label>
        </div>
      );
    }

    if (activeTool === "jwt") {
      return (
        <textarea
          className="tool-input"
          value={jwtInput}
          onChange={(event) => setJwtInput(event.target.value)}
          placeholder="paste JWT"
        />
      );
    }

    if (activeTool === "json") {
      return (
        <textarea
          className="tool-input"
          value={jsonInput}
          onChange={(event) => setJsonInput(event.target.value)}
          placeholder='{"json":"here"}'
        />
      );
    }

    if (activeTool === "base64") {
      return (
        <textarea
          className="tool-input"
          value={base64Input}
          onChange={(event) => setBase64Input(event.target.value)}
          placeholder="text or base64"
        />
      );
    }

    if (activeTool === "url") {
      return (
        <textarea
          className="tool-input"
          value={urlInput}
          onChange={(event) => setUrlInput(event.target.value)}
          placeholder="text or encoded URL fragment"
        />
      );
    }

    return (
      <div className="regex-grid">
        <input
          value={regexPattern}
          onChange={(event) => setRegexPattern(event.target.value)}
          placeholder="pattern"
        />
        <input
          value={regexFlags}
          onChange={(event) => setRegexFlags(event.target.value)}
          placeholder="flags"
        />
        <textarea
          value={regexSample}
          onChange={(event) => setRegexSample(event.target.value)}
          placeholder="sample text"
        />
        <input
          value={regexReplacement}
          onChange={(event) => setRegexReplacement(event.target.value)}
          placeholder="replacement (optional)"
        />
      </div>
    );
  }

  function renderActions() {
    if (activeTool === "guid" || activeTool === "lorem" || activeTool === "string") {
      return (
        <button className="primary-button" onClick={() => run("generate")}>
          generate
        </button>
      );
    }

    if (activeTool === "cpf" || activeTool === "cnpj") {
      return (
        <>
          <button className="primary-button" onClick={() => run("generate")}>
            generate
          </button>
          <button onClick={() => run("validate")}>validate</button>
        </>
      );
    }

    if (activeTool === "jwt") {
      return (
        <button className="primary-button" onClick={() => run("decode")}>
          decode
        </button>
      );
    }

    if (activeTool === "json") {
      return (
        <>
          <button className="primary-button" onClick={() => run("format")}>
            format
          </button>
          <button onClick={() => run("minify")}>minify</button>
        </>
      );
    }

    if (activeTool === "base64" || activeTool === "url") {
      return (
        <>
          <button className="primary-button" onClick={() => run("encode")}>
            encode
          </button>
          <button onClick={() => run("decode")}>decode</button>
        </>
      );
    }

    return (
      <button className="primary-button" onClick={() => run("run")}>
        run
      </button>
    );
  }
}
