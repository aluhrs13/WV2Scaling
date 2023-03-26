# WV2PerfScaling

The performance characteristics of WebView2 can be confusing. This application lets you interactively understand what happens when a new WV2 is created by making a grid of WV2s and showing the performance details. It also has a few suggestions for changing the behavior using different environment options.

## UI
- Textbox in the top-left is for the URL for new WV2s
- "Add WV2" button will create a new WV2 and navigate it to that URL.
- "Process List" list box will update after each creation with the current set of WV2 processes and their memory usage.
- "Creation Times" will update with every creation showing how long it took to create the WV2 and how long it took for the navigation to complete and the Largest contentful paint (LCP) on the page. LCP is generally a good number to look at for "When the user thinks the page is done loading".
- "Suspend All" will call WV2's `TrySuspend` method on all the WV2s. Unchecking will resume them.
- "Refresh" refreshes the Process list.

## Meanings
- "Creation starting" -> "Creation completed" is purely bound by the device and WV2.
- "Creation completed" -> "Navigation completed" is largely bound by the website/webapp and how long it takes to get the data for the site.
- "Navigation competed" -> "LCP Event" is almost entirely bound by the website/webapp's web perf.

Sometimes navigation competed and LCP come in a different order.
