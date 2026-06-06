import type { GuidFormat, LoremMode, ToolKind } from "../types";

type DevToolsViewProps = {
  activeTool: ToolKind;
  guidFormat: GuidFormat;
  stringLength: number;
  includeUppercase: boolean;
  includeLowercase: boolean;
  includeNumbers: boolean;
  includeSymbols: boolean;
  loremMode: LoremMode;
  loremAmount: number;
  generatedValue: string;
  onToolChange: (tool: ToolKind) => void;
  onGuidFormatChange: (format: GuidFormat) => void;
  onStringLengthChange: (length: number) => void;
  onIncludeUppercaseChange: (value: boolean) => void;
  onIncludeLowercaseChange: (value: boolean) => void;
  onIncludeNumbersChange: (value: boolean) => void;
  onIncludeSymbolsChange: (value: boolean) => void;
  onLoremModeChange: (mode: LoremMode) => void;
  onLoremAmountChange: (amount: number) => void;
  onGenerate: () => void;
  onCopy: () => void;
};

export function DevToolsView({
  activeTool,
  guidFormat,
  stringLength,
  includeUppercase,
  includeLowercase,
  includeNumbers,
  includeSymbols,
  loremMode,
  loremAmount,
  generatedValue,
  onToolChange,
  onGuidFormatChange,
  onStringLengthChange,
  onIncludeUppercaseChange,
  onIncludeLowercaseChange,
  onIncludeNumbersChange,
  onIncludeSymbolsChange,
  onLoremModeChange,
  onLoremAmountChange,
  onGenerate,
  onCopy,
}: DevToolsViewProps) {
  return (
    <section className="dev-drawer">
      <div className="drawer-title">
        <strong>./tools.sh</strong>
      </div>

      <div className="tool-tabs">
        <button
          className={activeTool === "guid" ? "active" : ""}
          onClick={() => onToolChange("guid")}
        >
          guid
        </button>
        <button
          className={activeTool === "string" ? "active" : ""}
          onClick={() => onToolChange("string")}
        >
          string
        </button>
        <button
          className={activeTool === "lorem" ? "active" : ""}
          onClick={() => onToolChange("lorem")}
        >
          lorem
        </button>
      </div>

      {activeTool === "guid" && (
        <div className="segmented-control">
          <button
            className={guidFormat === "default" ? "active" : ""}
            onClick={() => onGuidFormatChange("default")}
          >
            default
          </button>
          <button
            className={guidFormat === "no-hyphens" ? "active" : ""}
            onClick={() => onGuidFormatChange("no-hyphens")}
          >
            compact
          </button>
          <button
            className={guidFormat === "uppercase" ? "active" : ""}
            onClick={() => onGuidFormatChange("uppercase")}
          >
            upper
          </button>
        </div>
      )}

      {activeTool === "string" && (
        <div className="tool-options">
          <label>
            len
            <input
              type="number"
              min={1}
              max={512}
              value={stringLength}
              onChange={(event) =>
                onStringLengthChange(Number(event.target.value))
              }
            />
          </label>
          <label>
            <input
              type="checkbox"
              checked={includeUppercase}
              onChange={(event) =>
                onIncludeUppercaseChange(event.target.checked)
              }
            />
            A-Z
          </label>
          <label>
            <input
              type="checkbox"
              checked={includeLowercase}
              onChange={(event) =>
                onIncludeLowercaseChange(event.target.checked)
              }
            />
            a-z
          </label>
          <label>
            <input
              type="checkbox"
              checked={includeNumbers}
              onChange={(event) => onIncludeNumbersChange(event.target.checked)}
            />
            0-9
          </label>
          <label>
            <input
              type="checkbox"
              checked={includeSymbols}
              onChange={(event) => onIncludeSymbolsChange(event.target.checked)}
            />
            sym
          </label>
        </div>
      )}

      {activeTool === "lorem" && (
        <div className="tool-options lorem-options">
          <label>
            mode
            <select
              value={loremMode}
              onChange={(event) =>
                onLoremModeChange(event.target.value as LoremMode)
              }
            >
              <option value="words">words</option>
              <option value="characters">chars</option>
            </select>
          </label>
          <label>
            count
            <input
              type="number"
              min={1}
              max={loremMode === "characters" ? 5000 : 500}
              value={loremAmount}
              onChange={(event) =>
                onLoremAmountChange(Number(event.target.value))
              }
            />
          </label>
        </div>
      )}

      <div className={`generated-output output-${activeTool}`}>
        {generatedValue || "<waiting for output>"}
      </div>

      <div className="tool-actions">
        <button className="primary-button" onClick={onGenerate}>
          generate
        </button>
        <button disabled={!generatedValue} onClick={onCopy}>
          copy
        </button>
      </div>
    </section>
  );
}
