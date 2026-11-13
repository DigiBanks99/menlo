# Statement export formats of South African retail banks

Research for [#598](https://github.com/DigiBanks99/menlo/issues/598), which feeds the Spend Requirement's statement import and how spends avoid being recorded twice. Researched November 2026. Pending owner review.

## Method and confidence

- Only first-party sources count as **verified**: the bank's own help pages, user guides, published file specifications and API references. Each verified claim links its source.
- Third-party converter blogs, forum posts and how-to videos were read for leads only. Anything resting on them alone is marked **unverified**.
- Bank help pages describe menus, not file contents. No sample export file was published by any bank for its *personal* channel, so field lists for personal-channel CSV/OFX are unverified unless a specification says otherwise.
- Some bank sites refused automated reads (Capitec returned HTTP 403 to every request; the Standard Bank community forum did not resolve). Claims that depend on those pages are marked unverified.
- The owner's own working statement parser counts as verified evidence for the layouts it reads, and is marked **owner evidence**.
- Business-banking platforms (FNB Online Banking Enterprise, Standard Bank OB4B, Absa Business Integrator) are documented where they are the only primary source, but a household will normally use the personal channel, which may offer less.

## Summary

| Bank | Machine-readable export (personal) | How far back | Stable transaction ID | Card merchant name |
| --- | --- | --- | --- | --- |
| Absa | Unverified. Help pages mention "select file format" but never name one | Unverified | None documented | Unverified |
| Capitec | Unverified (personal). Business: CSV and OFX per Capitec's business page, unverified (page blocked) | Statements 1-3 months free in app (unverified) | None documented | Unverified |
| FNB | OFX (Microsoft Money) and CSV are specified for the Online Banking Enterprise channel; personal CSV/OFX unverified; personal password-protected PDF statements confirmed by the owner's working parser | Enterprise transaction history: 180 days | OFX `FITID` = posting date + daily sequence number (Enterprise spec) | Description is the 42-character statement narrative; no separate merchant field |
| Nedbank | **Verified**: CSV, OFX, OFC, QIF, Excel from Online Banking (web) | Unverified ("select a period") | None documented for CSV; OFX `FITID` content unverified | Unverified |
| Standard Bank | Personal: unverified (PDF statements verified, 24 months). Business (OB4B): CSV, OFX, QFX, QBO, PDF, XLSX | Personal statements: 24 months | None documented; forum reports of non-standard `FITID` (unverified) | Unverified |
| Discovery Bank | **Verified**: Excel or CSV transaction list from the app, emailed | Unverified | None documented | Smart Search matches on merchant name, so the description carries it; column layout unverified |
| TymeBank (now GoTyme Bank) | None found. PDF statements emailed from the app | 12 months of statements | n/a | n/a |
| Investec | **Verified API** (Programmable Banking, JSON). File exports from Investec Online unverified | API defaults to the last 180 days; earlier `fromDate` accepted, limit undocumented | `uuid` (since 2024-11-07), derived from account, posting date and `postedOrder`; posted transactions only | `description` plus masked `cardNumber`; no separate merchant field |

## Per bank

### Absa

- **Formats.** Absa's help page for the app says, for credit card statements, "Select date range and tap OK", then "Select file format", and for home loan e-stamped statements "Select file format in which to open the statement" ([Absa: how to manage accounts online](https://www.absa.co.za/help/how-to-manage-accounts-online/)). The formats on offer are not named. Online Banking archive statements are downloaded after accepting a charge (same page).
- eStatements are password-protected PDFs ([Absa eStatements](https://www.absa.co.za/self-service/digital-banking-services/anytime-anywhere-banking-transactions/estatements/)).
- **Transaction history.** Online Banking shows a date-range picker for older transactions ([Absa help: older transactions](https://onlinebanking.help.absa.co.za/en/help/transaction-history/statements/how-do-i-see-my-older-transactions/)); no export or retention limit is stated.
- **Unverified.** Accounting-software forum posts say Absa Online Banking downloads OFX/OFC files; no Absa page confirms this. No field list, ID or merchant information was found.

### Capitec

- Capitec's site returned HTTP 403 to every automated request, so nothing below could be read first-hand. All of it is **unverified**.
- Search snippets of Capitec pages say: app statements are shared as PDF ("Share PDF Statement", `https://www.capitecbank.co.za/were-here-to-help/how-to/how-to-email-statements-from-your-phone/`); a 1-3 month statement is free in the app; and the *business* accounting-integration page offers transaction-history export as CSV or OFX (`https://www.capitecbank.co.za/business/transact/accounting-package-integration/`).
- No personal-account CSV/OFX export, field list or transaction ID was found.

### FNB

- **Business channel (verified).** FNB Online Banking Enterprise publishes statement file specifications for CSV, OFX, QIF, OFC, PASTEL, SAP, MT940 and others ([FNB OBE: Statements](https://www.online.fnb.co.za/rhelp_0_15/Downloads/Statements.htm)). Its user guide says "Transaction history available for up to 180 days" ([FNB OBE South Africa user guide, 2017](https://www.online.fnb.co.za/rhelp_0_15/Downloads/User_Guide/South_Africa.pdf)).
- **CSV spec** ([Statement Type - CSV, July 2012](https://www.online.fnb.co.za/rhelp_0_15/Downloads/Statement_File_Specifications/Statement_Type_-_CSV.pdf)):
  - Header: statement date (`CCYYMMDD`), account nickname, account number (11 digits, leading zeros), opening and closing balance ("-" if in debit), totals and counts of debits and credits.
  - Transactions: effective date (`CCYYMMDD`), 42-character description ("the same as the description on the paper based statement"), 30-character reference, service fee with a debit/credit indicator, amount ("-" for a debit), calculated balance.
  - No transaction ID column.
- **OFX spec** ([Statement Type - OFX, July 2012](https://www.online.fnb.co.za/rhelp_0_15/Downloads/Statement_File_Specifications/Statement_Type_-_OFX.pdf)): OFX 1.0.2 SGML for Microsoft Money; `BANKID` FNB; `ACCTTYPE` hard-coded `CHECKING`; per transaction `TRNTYPE` (DEBIT/CREDIT), `DTPOSTED` (`CCYYMMDD`), `TRNAMT` (negative for debits), `FITID`, `CHECKNUM`, `MEMO` (the 42-character statement narrative plus the 30-character client reference); `LEDGERBAL` closing balance.
  - `FITID` is described as "Unique Identifier for each transaction. Prevents duplication on import. This is made up of a date and a sequential numeric", for example `201011271`, `201011272`. It is a position within a posting day, not an identifier carried by the transaction.
- **Personal channel (unverified).** The FNB app guide only shows downloading statements, without naming a format ([FNB app: Download statements](https://www.fnb.co.za/downloads/demos/App_HowTo/lastest/STATEMENTS_GUIDE.pdf)). Third-party guides describe a "Download" button on personal Online Banking transaction history offering "Microsoft Money (OFX)" and CSV, web only, over a 90-day default view; no FNB page confirming this was found. Whether the personal files follow the 2012 Enterprise specs is unknown.
- **Personal channel PDF statements (owner evidence).** The owner has a working parser, outside this repo, that reads FNB personal statements. It reads them as **password-protected PDFs** (text extracted with PdfPig and grouped into lines by position). That is first-hand evidence that the PDF statement is a usable personal-channel source. It doesn't settle whether personal CSV or OFX exports exist. The parser handles two line layouts:
  - **Credit card:** `dd MMM`, then the description, then the amount, e.g. `08 May <merchant> <town> ZA 92.00`.
  - **Cheque account:** `dd MMM`, then the description, a masked card reference, a reference date (`dd MMM`), the amount, the running balance and a `Cr`/`Dr` marker.
  - **No year on dates:** the year has to be inferred from the statement period, and December entries on a January statement belong to the previous year.
  - **Description prefixes:** descriptions carry a transaction-type prefix such as `POS Purchase`. The merchant name is free text inside the description.
- Note: help pages at `ecorp-fnb.fnb-online.com` that turn up in searches belong to First National Bank of Pennsylvania (US), not FNB South Africa.

### Nedbank

- **Formats (verified).** Nedbank's Online Banking how-to guide, section 4c "Access transaction listings in various file formats (CSV, OFX, OFC, QIF, XLSX)": log in, select the account, Statements and documents, Other, "Select a period", then "You now have the option to download your statement in CSV, OFX, OFC, QIF or Excel format" ([Nedbank Online Banking How To Guide, Sept 2020](https://www.nedbank.co.za/content/dam/nedbank/site-assets/Personal/Digital/OnlineBanking/Nedbank%20Online%20Banking%20How%20To%20Guide%20Sept2020.pdf)). This is the personal web channel. Provisional and bank-stamped statements are PDF (same guide, 4a and 4b).
- The guide's screenshots show a transaction list with Date, Description, Amount (negative for debits) and Balance, but no export file layout is published.
- **Unverified.** How far back the period picker goes; CSV columns; whether OFX carries a stable `FITID`; whether the Money app offers the same export (third-party sources say app statements are PDF only).

### Standard Bank

- **Personal (partly verified).** Internet Banking statements are under Transact, History & Documents, Statements, "limited to 24 months back", and can be downloaded or viewed ([Standard Bank FAQ](https://www.standardbank.co.za/southafrica/personal/contact-us/frequently-asked-questions)). The app offers a 6-month statement and previous monthly statements, downloaded or emailed ([Standard Bank: bank account documents](https://www.standardbank.co.za/southafrica/personal/products-and-services/ways-to-bank/help-centre/bank-account-documents)). Neither page names a file format.
- **Business (verified).** Online Banking for Business (OB4B) downloads transaction history as CSV, OFX, QFX, QBO, PDF or XLSX. CSV comes in two variants: `Transactions_without_balances.csv` (for Sage, QuickBooks) and `Transactions_with_balances.csv` (with running balances) ([OB4B how-to guide: downloading transaction history](https://www.standardbank.co.za/static_file/South%20Africa/PDF/How%20To%20Guides/OB4B%20How-to%20guide%20-%20Downloading%20transaction%20history.pdf)). Columns are not listed.
- **Unverified.** A Standard Bank community-forum post describes a personal Internet Banking "Download" drop-down with CSV, PDF, QIF, OFX, XLSX and TXT; another user in the same thread saw only PDF. A forum thread titled "OFX FITID not valid" reports Standard Bank OFX files whose `FITID` does not follow the OFX standard. The forum could not be reached to confirm either.

### Discovery Bank

- **Formats (verified).** In the app, open the account, tap the top-right menu, "Download transactions", "Complete the date range and choose a file format", tap Send ([Discovery Bank: managing your accounts](https://www.discovery.co.za/bank/info-and-tips-managing-your-accounts)). Smart Search lists can be exported: "You can even download and export your lists in the format of your choice (Excel or CSV)", and the file is "sent to the email address on your profile" ([Discovery Bank blog, January 2024](https://www.discovery.co.za/bank/blog-paperless-finances-with-smart-vault)).
- **Merchant.** "Smart Search lets you search for key phrases or merchant names to find a specific transaction" (same blog), so card transactions carry a merchant name in the description. Its exact form in the CSV is unverified.
- Monthly statements are loaded to the app each month (managing-your-accounts page above). A search snippet of an older platform-migration FAQ says statements can no longer be downloaded in Excel, only PDF on the web ([Moving to Discovery Bank FAQs](https://www.discovery.co.za/assets/discoverycoza/bank/legal/moving-to-discovery-bank-faqs.pdf); not read first-hand, unverified).
- **Unverified.** CSV columns, sign convention, how far back, any transaction ID.

### TymeBank (now GoTyme Bank)

- TymeBank's site redirects to GoTyme Bank. In the app: "go to Menu, then select Bank Statement or Proof of Account"; "Your documents will be sent to your email address within minutes" ([GoTyme Bank FAQs](https://gotyme.co.za/personal/help/faqs)).
- The account page lists 12 months of statements via app and email (from search snippet of the TymeBank everyday-account page; unverified first-hand). Internet banking is no longer offered (search snippet; unverified).
- No CSV, OFX or API was found. Treat as PDF only.

### Investec

- **API (verified).** Programmable Banking, Private Bank account information API: `GET /za/pb/v1/accounts/{accountId}/transactions` with `fromDate` ("Will default to today's date, minus 180 days, if not specified"), `toDate`, `transactionType` (for example `CardPurchases`, `FeesAndInterest`) and `includePending` ([Investec API reference: SA PB Account Information](https://developer.investec.com/api-reference/SA%20PB%20Account%20Information)). The reference says it covers Private Bank accounts "whether for personal or business use".
- **Fields** (same reference): `accountId`, `type` (`CREDIT`/`DEBIT`), `transactionType`, `status` (`POSTED`/`PENDING`), `description`, `cardNumber` (masked, empty for non-card), `postedOrder` (number), `postingDate`, `valueDate`, `actionDate`, `transactionDate`, `amount`, `runningBalance`, `uuid` ("Unique identifier for the transaction entry").
  - Sign convention: in the reference's sample, `amount` is positive for both debits and credits; direction comes from `type`.
  - `transactionDate` is when the card was swiped and `postingDate` when it hit the balance ([Investec community wiki](https://investec.gitbook.io/programmable-banking-community-wiki/get-started/api-quick-start-guide/how-to-get-your-transaction-history)).
- **ID.** The API changelog lists "Added a uuid property to the transactions endpoint" on 2024-11-07 and `postedOrder` on 2020-11-10 (API reference changelog). In the reference's sample, `uuid` `32224202006110013379` is the last five digits of the account ID, the posting date and `postedOrder` padded to seven digits. A changelog copy says it is "only populated for posted transaction on Private Bank Accounts" and "is not a backend banking generated ID and will change if any of the properties making it up changes" ([investec-za-pb package page](https://libraries.io/pypi/investec-za-pb), quoting the changelog; the official wording was not confirmed first-hand).
  - A community-reported savings-account entry had `postedOrder` 0 (unverified).
- **Files.** Investec Online file exports (CSV/OFX) were not found on any South African Investec page. Investec's UK help pages say statements are PDF; those don't necessarily apply to SA. PDF statements and tax certificates are also available through the API's documents endpoint (same reference).

## OFX `FITID` and duplicate detection

The OFX specification defines `FITID` as the financial institution's transaction ID, unique within one account at one institution, and says its "primary purpose is to allow a client to detect duplicate responses". It is up to 255 characters, with 32 or fewer recommended. These are quotations of the specification found in GnuCash and KMyMoney mailing-list posts ([GnuCash list](https://lists.gnucash.org/pipermail/gnucash-user/2005-November/015180.html), [KMyMoney bug 390681](https://bugs.kde.org/show_bug.cgi?id=390681)); they were not checked against the specification text on ofx.net. The same KMyMoney report notes that some banks issue a different `FITID` each time the same statement is downloaded.

## What this means for statement import

- **Machine-readable exports confirmed by the bank:** Nedbank (CSV/OFX/QIF/Excel, web), Discovery Bank (CSV/Excel, app, emailed) and Investec (JSON API). FNB and Standard Bank document CSV/OFX for their business platforms only. Absa, Capitec and TymeBank have no confirmed personal-channel export, so PDF parsing may be the only route for them. For FNB personal accounts, parsing the password-protected PDF statement is proven to work (owner evidence), so PDF import is a realistic first route, not just a fallback.
- **Stable IDs:** only Investec documents a per-transaction ID (`uuid`), and it is derived, posted-only and may change. FNB's documented `FITID` is a date plus a sequence number within that day. No CSV format found carries an ID.
- **Main caveat for duplicate detection:** no bank's ID can be relied on as a permanent key. Investec's `uuid` is empty for pending transactions, which may change before they post. Sequence numbers depend on posting order, and descriptions are truncated (FNB: 42 characters). Duplicate detection should match on account, posting date, amount and normalized description, using a bank ID only as a hint where one exists.
- **Merchant names:** no source documents a separate merchant field. Merchants appear inside a truncated description (Discovery's Smart Search confirms merchant names are present; Investec adds a masked card number).

## Open questions for the owner

- Personal-channel exports for Absa, Capitec, FNB and Standard Bank need to be checked in a signed-in session. A sample file from each, with personal data removed, would settle the columns, sign convention and `FITID` behaviour.
- Does re-downloading the same period from Nedbank or FNB personal banking return the same `FITID`s?
