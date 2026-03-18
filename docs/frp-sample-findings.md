# FRP Sample Findings (User Shared Files)

Date: 2026-03-18
Source folder: C:/Users/ilker/OneDrive/Masaustu/Yeni klasor (2)
Sample size: 18 .frp files

## Confirmed XML Pattern

- Root tag is `TfrxReport` (not `Report`).
- Report metadata is primarily stored on root attributes:
  - `ReportOptions.Name`
  - `ReportOptions.CreateDate`
  - `ReportOptions.LastChange`
- Script content is commonly stored on root attribute:
  - `ScriptText.Text`
- SQL content is stored in query component attributes:
  - `SQL.Text` on nodes such as `TfrxFOQuery`.

## Parser Implications

- Looking only for `SelectCommand` misses SQL in these samples.
- Looking only for `<ScriptText>` nodes misses script in these samples.
- Looking for `<ReportInfo>` misses metadata in these samples.
- Date values like `40156,8543971296` are OA date serials and should be parsed accordingly.

## Implemented Adjustments

- Parser now reads:
  - SQL from `SQL.Text` and `SelectCommand`
  - Script from `ScriptText.Text` and `ScriptText` nodes
  - Metadata from `ReportOptions.*` plus existing fallbacks
  - OA serial date values for create/modify timestamps
- Export/save now writes back to:
  - `SQL.Text` and `SelectCommand`
  - `ScriptText.Text` and `ScriptText` nodes
