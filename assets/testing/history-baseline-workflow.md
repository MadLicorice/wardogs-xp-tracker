# WARDOGS XP Tracker save

This file contains local app data. Keep the JSON block intact.

```json
{
  "Format": "WardogsXpTracker.MarkdownSave",
  "SchemaVersion": 1,
  "Data": {
    "SchemaVersion": 1,
    "SeasonId": "c137d6db-3834-4bce-b1e4-b801a5b67463",
    "Name": "MOCK \u2014 Saved starting-baseline confirmation",
    "Sessions": [
      {
        "Id": "0e5cc5df-d57b-4a9a-a714-29490e68d3f9",
        "StartedAt": "2026-10-17T15:00:00+00:00",
        "EndedAt": "2026-10-17T15:25:00+00:00",
        "State": "Ended",
        "Outcome": "Finished",
        "RulesSnapshot": {
          "Id": "6052b351-2d66-4de1-8d62-f03a037b9a2e",
          "Version": 1,
          "PatchDate": "2026-10-15",
          "Name": "Oct 15 2026 Patch XP Bonus Rules",
          "Tiers": [
            {
              "MinimumXp": 0,
              "Third": 0,
              "Second": 0,
              "First": 0
            },
            {
              "MinimumXp": 500,
              "Third": 600,
              "Second": 700,
              "First": 800
            },
            {
              "MinimumXp": 1000,
              "Third": 1200,
              "Second": 1400,
              "First": 1600
            },
            {
              "MinimumXp": 1500,
              "Third": 1800,
              "Second": 2100,
              "First": 2400
            },
            {
              "MinimumXp": 2000,
              "Third": 2400,
              "Second": 2800,
              "First": 3200
            },
            {
              "MinimumXp": 2500,
              "Third": 3000,
              "Second": 3500,
              "First": 4000
            }
          ]
        },
        "Tracks": {
          "Assault": {
            "Class": "Assault",
            "Enabled": true,
            "Baseline": {
              "Id": "ea7b7b74-63b2-4c22-918b-77874c8c1e52",
              "Version": 1,
              "Xp": 10000,
              "At": "2026-10-17T15:06:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "Unknown",
              "Validity": "Unknown",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "ea7b7b74-63b2-4c22-918b-77874c8c1e52",
              "Version": 1,
              "Xp": 10000,
              "At": "2026-10-17T15:06:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "Unknown",
              "Validity": "Unknown",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "bce74346-0527-4ad4-af44-791e83d090a9",
              "Class": "Assault",
              "Xp": 12500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:13:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 12500/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "bce74346-0527-4ad4-af44-791e83d090a9",
              "Class": "Assault",
              "Xp": 12500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:13:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 12500/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": null,
            "TargetReachedAt": "2026-10-17T15:13:00+00:00",
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": true,
            "Baseline": {
              "Id": "07d99a6b-c59e-4266-bf23-b98293c412f2",
              "Version": 1,
              "Xp": 20000,
              "At": "2026-10-17T15:11:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "Unknown",
              "Validity": "Unknown",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "07d99a6b-c59e-4266-bf23-b98293c412f2",
              "Version": 1,
              "Xp": 20000,
              "At": "2026-10-17T15:11:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "Unknown",
              "Validity": "Unknown",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "24728628-62bc-4c91-b837-68f7a5c23fe6",
              "Class": "Medic",
              "Xp": 22000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:18:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 22000/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "24728628-62bc-4c91-b837-68f7a5c23fe6",
              "Class": "Medic",
              "Xp": 22000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:18:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 22000/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Recon": {
            "Class": "Recon",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Support": {
            "Class": "Support",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Driver": {
            "Class": "Driver",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Pilot": {
            "Class": "Pilot",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          }
        },
        "History": [
          {
            "Id": "ebc2b962-3c5c-4119-af40-91410526f4c0",
            "Kind": "SessionStarted",
            "At": "2026-10-17T15:01:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "New session. Capture each enabled class with F3 or enter its baseline manually.",
            "Observation": null
          },
          {
            "Id": "69547af9-f33c-48d1-8538-5aa28d26f218",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T15:02:00+00:00",
            "Class": "Recon",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Recon tracking disabled.",
            "Observation": null
          },
          {
            "Id": "108cc1dd-c871-4830-8e90-6da5369cecac",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T15:03:00+00:00",
            "Class": "Support",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Support tracking disabled.",
            "Observation": null
          },
          {
            "Id": "e0e43585-f096-48dc-90cf-238bbad46d9f",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T15:04:00+00:00",
            "Class": "Driver",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Driver tracking disabled.",
            "Observation": null
          },
          {
            "Id": "9282d0d8-10a1-49c9-9f5e-d083be7f3deb",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T15:05:00+00:00",
            "Class": "Pilot",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Pilot tracking disabled.",
            "Observation": null
          },
          {
            "Id": "fa2c8dde-5a39-4032-87f6-01307c5e752b",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T15:08:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "An UNLOCK scene was observed in this session.",
            "Observation": null
          },
          {
            "Id": "57c1adc8-98f6-4534-88c5-ffc2696c7d18",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T15:09:00+00:00",
            "Class": "Assault",
            "Xp": 10000,
            "PreviousXp": null,
            "BaselineId": "ea7b7b74-63b2-4c22-918b-77874c8c1e52",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "5372bbdd-08ef-4082-a14e-d470c3c6b815",
              "Class": "Assault",
              "Xp": 10000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:06:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 10000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "6fcc4559-53cc-43fa-a9fc-33ab8c23fead",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T15:12:00+00:00",
            "Class": "Medic",
            "Xp": 20000,
            "PreviousXp": null,
            "BaselineId": "07d99a6b-c59e-4266-bf23-b98293c412f2",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "84c1bf99-daa8-4290-a1e9-670cfbf287a3",
              "Class": "Medic",
              "Xp": 20000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:11:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 20000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "fa343d05-c6eb-4631-bf61-23aefed43c5c",
            "Kind": "CurrentXp",
            "At": "2026-10-17T15:15:00+00:00",
            "Class": "Assault",
            "Xp": 12500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "bce74346-0527-4ad4-af44-791e83d090a9",
              "Class": "Assault",
              "Xp": 12500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:13:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 12500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "8125e38e-6c2d-4a78-afbd-955cfde9ef69",
            "Kind": "MaximumTierReached",
            "At": "2026-10-17T15:16:00+00:00",
            "Class": "Assault",
            "Xp": 12500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Observed earned XP reached the maximum bonus tier. Bonus receipt is separate.",
            "Observation": {
              "Id": "bce74346-0527-4ad4-af44-791e83d090a9",
              "Class": "Assault",
              "Xp": 12500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:13:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 12500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "ed7cc8aa-73b8-40f2-ab5a-69516f1cc7a2",
            "Kind": "CurrentXp",
            "At": "2026-10-17T15:19:00+00:00",
            "Class": "Medic",
            "Xp": 22000,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "24728628-62bc-4c91-b837-68f7a5c23fe6",
              "Class": "Medic",
              "Xp": 22000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:18:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 22000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "7d3cde20-6c5d-4b90-a540-ea2ec5a0e55f",
            "Kind": "EndDetected",
            "At": "2026-10-17T15:26:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Live earned-XP checks froze before final totals were collected.",
            "Observation": null
          },
          {
            "Id": "807f24dc-72ef-4c94-812c-cc89f6513da3",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T15:30:00+00:00",
            "Class": "Assault",
            "Xp": 16500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "98a1b4b3-6b9a-4663-9414-815ea5454dc5",
              "Class": "Assault",
              "Xp": 16500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:20:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 16500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "f2867e12-66f2-4c6d-b6bc-edb91b38356e",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T15:32:00+00:00",
            "Class": "Medic",
            "Xp": 25200,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "3c3f13ac-bec6-4149-b5c1-8f3f4186e38c",
              "Class": "Medic",
              "Xp": 25200,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:21:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 25200/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "b16efc44-0958-45d2-8c78-b21c0418ff22",
            "Kind": "FinalConfirmed",
            "At": "2026-10-17T15:35:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Observed final totals confirmed. Live earned-XP checks were not advanced.",
            "Observation": null
          },
          {
            "Id": "1621633b-942f-4e6d-b438-69c8f878b182",
            "Kind": "DemoScenario",
            "At": "2026-10-17T15:36:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "MOCK: completed first-place match with unconfirmed Assault and Medic starting baselines. In Saved Results, open Starting baselines. Confirm Assault, then confirm all remaining eligible baselines. Reopen Summary to see the supported 4,500 in-match XP and 7,200 bonus XP.",
            "Observation": null
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": {
          "SessionId": "0e5cc5df-d57b-4a9a-a714-29490e68d3f9",
          "FirstObservedAt": "2026-10-17T15:27:00+00:00",
          "LastObservedAt": "2026-10-17T15:31:00+00:00",
          "ConfirmedAt": "2026-10-17T15:34:00+00:00",
          "Tracks": {
            "Assault": {
              "Id": "98a1b4b3-6b9a-4663-9414-815ea5454dc5",
              "Class": "Assault",
              "Xp": 16500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:20:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 16500/1000000XP",
              "Confidence": 1
            },
            "Medic": {
              "Id": "3c3f13ac-bec6-4149-b5c1-8f3f4186e38c",
              "Class": "Medic",
              "Xp": 25200,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:21:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 25200/1000000XP",
              "Confidence": 1
            }
          },
          "PossiblePlaces": [
            1
          ],
          "PlacementEvidence": [
            {
              "At": "2026-10-17T15:33:00+00:00",
              "Source": "Manual",
              "PossiblePlaces": [
                1
              ],
              "RawText": "User-confirmed placement"
            }
          ],
          "HasConflict": false,
          "ConflictingTracks": [],
          "KeepOldReferencesChosen": false
        },
        "FinalObservations": [
          {
            "SessionId": "0e5cc5df-d57b-4a9a-a714-29490e68d3f9",
            "FirstObservedAt": "2026-10-17T15:22:00+00:00",
            "LastObservedAt": "2026-10-17T15:24:00+00:00",
            "ConfirmedAt": null,
            "Tracks": {
              "Assault": {
                "Id": "98a1b4b3-6b9a-4663-9414-815ea5454dc5",
                "Class": "Assault",
                "Xp": 16500,
                "NextThreshold": 1000000,
                "CurrentLevel": null,
                "ThresholdLevel": null,
                "At": "2026-10-17T15:20:00+00:00",
                "Source": "AutomaticOcr",
                "Screen": "EndProgression",
                "RawText": "MOCK 16500/1000000XP",
                "Confidence": 1
              },
              "Medic": {
                "Id": "3c3f13ac-bec6-4149-b5c1-8f3f4186e38c",
                "Class": "Medic",
                "Xp": 25200,
                "NextThreshold": 1000000,
                "CurrentLevel": null,
                "ThresholdLevel": null,
                "At": "2026-10-17T15:21:00+00:00",
                "Source": "AutomaticOcr",
                "Screen": "EndProgression",
                "RawText": "MOCK 25200/1000000XP",
                "Confidence": 1
              }
            },
            "PossiblePlaces": [],
            "PlacementEvidence": [],
            "HasConflict": false,
            "ConflictingTracks": [],
            "KeepOldReferencesChosen": false
          }
        ],
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      }
    ],
    "ActiveSessionId": "0e5cc5df-d57b-4a9a-a714-29490e68d3f9",
    "EnabledTracks": {
      "Assault": true,
      "Medic": true,
      "Recon": false,
      "Support": false,
      "Driver": false,
      "Pilot": false
    }
  }
}
```
