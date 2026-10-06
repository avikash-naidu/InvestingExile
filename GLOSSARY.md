# InvestingExile

A league-start investment tracker for Path of Exile: which items are worth buying when a league starts, judged from poe.ninja prices and later patch notes.

## Language

**League**:
An active temporary challenge league that poe.ninja is currently tracking. The permanent leagues (Standard and Hardcore) are out of scope; the tracker ignores them by convention, not by a hardcoded exclusion.
_Avoid_: Season, realm

**League-start window**:
The roughly first two weeks of a League, when prices move fastest and the tracker ingests the current League's prices on a cadence. Outside this window the current League is not pulled regularly.
_Avoid_: Launch period, early game

**Past League**:
A League that has already ended. Its prices are no longer served by the live poe.ninja economy API and can only be recovered from poe.ninja's CSV data dumps. The tracker builds history by ingesting each League while it is active.
_Avoid_: Old league, previous season
