# WARDOGS XP Tracker save

This file contains local app data. Keep the JSON block intact.

```json
{
  "Format": "WardogsXpTracker.MarkdownSave",
  "SchemaVersion": 1,
  "Data": {
    "SchemaVersion": 1,
    "SeasonId": "3a5139bc-7099-4f61-b211-e7751ea9f0aa",
    "Name": "DEMO \u2014 Analytics examples",
    "Sessions": [
      {
        "Id": "e0960cc5-bd21-4dc8-acfe-424f7f446554",
        "StartedAt": "2026-10-17T12:00:00+00:00",
        "EndedAt": "2026-10-17T12:16:00+00:00",
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
              "Id": "d7fb871d-9c3c-4aec-bdd1-b7c46a4b93b9",
              "Version": 1,
              "Xp": 10000,
              "At": "2026-10-17T12:08:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "d7fb871d-9c3c-4aec-bdd1-b7c46a4b93b9",
              "Version": 1,
              "Xp": 10000,
              "At": "2026-10-17T12:08:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "c3fd180c-ccbf-4ed3-80f4-f3cd471ab1ab",
              "Class": "Assault",
              "Xp": 12500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:12:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 12500/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "c3fd180c-ccbf-4ed3-80f4-f3cd471ab1ab",
              "Class": "Assault",
              "Xp": 12500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:12:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 12500/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": null,
            "TargetReachedAt": "2026-10-17T12:12:00+00:00",
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
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
            "Id": "b9517d96-3fc3-41b4-930f-0db077d1846e",
            "Kind": "SessionStarted",
            "At": "2026-10-17T12:01:00+00:00",
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
            "Id": "f3f71a27-7cdf-4755-a25d-72700ef56bf2",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T12:02:00+00:00",
            "Class": "Medic",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Medic tracking disabled.",
            "Observation": null
          },
          {
            "Id": "d8f46543-3af6-4f1b-8bda-fd38f9714b22",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T12:03:00+00:00",
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
            "Id": "74894a93-528b-4e5f-a6b5-a25642ada2df",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T12:04:00+00:00",
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
            "Id": "c8cd75f9-da59-4fce-8adb-86b347178b31",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T12:05:00+00:00",
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
            "Id": "e6c61ec3-d86e-4b1a-bc08-e89ad551fa07",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T12:06:00+00:00",
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
            "Id": "c1c2f144-5759-4fee-bc6d-0a21553f8350",
            "Kind": "DemoScenario",
            "At": "2026-10-17T12:07:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "01 \u00B7 Exact first place: Assault actions 2,500 \u002B bonus 4,000",
            "Observation": null
          },
          {
            "Id": "7758ea57-8242-4061-a083-05f9cdb883bb",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T12:09:00+00:00",
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
            "Id": "510c1c21-552b-4dcf-b806-6ccf8abcf92a",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T12:10:00+00:00",
            "Class": "Assault",
            "Xp": 10000,
            "PreviousXp": null,
            "BaselineId": "d7fb871d-9c3c-4aec-bdd1-b7c46a4b93b9",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "54874b5f-c110-4f0f-946d-7813367b4399",
              "Class": "Assault",
              "Xp": 10000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:08:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 10000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "1ae0f0ba-60a3-4789-a6d6-2a41ad0eed9d",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T12:11:00+00:00",
            "Class": "Assault",
            "Xp": 10000,
            "PreviousXp": null,
            "BaselineId": "d7fb871d-9c3c-4aec-bdd1-b7c46a4b93b9",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "f9723836-7e51-4135-982e-9e5d1e3f549c",
            "Kind": "CurrentXp",
            "At": "2026-10-17T12:13:00+00:00",
            "Class": "Assault",
            "Xp": 12500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "c3fd180c-ccbf-4ed3-80f4-f3cd471ab1ab",
              "Class": "Assault",
              "Xp": 12500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:12:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 12500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "e3c3eecb-986a-4276-9737-86e6aeffece4",
            "Kind": "MaximumTierReached",
            "At": "2026-10-17T12:14:00+00:00",
            "Class": "Assault",
            "Xp": 12500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Observed earned XP reached the maximum bonus tier. Bonus receipt is separate.",
            "Observation": {
              "Id": "c3fd180c-ccbf-4ed3-80f4-f3cd471ab1ab",
              "Class": "Assault",
              "Xp": 12500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:12:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 12500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "a1ca64ac-2a14-4905-a064-be0e26a79419",
            "Kind": "EndDetected",
            "At": "2026-10-17T12:17:00+00:00",
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
            "Id": "f1976fe9-1c6c-48a6-9f03-564b859bf86f",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T12:21:00+00:00",
            "Class": "Assault",
            "Xp": 16500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "5dbfe7bf-833c-447c-be32-ecc0ca5f04fd",
              "Class": "Assault",
              "Xp": 16500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:15:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 16500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "a550ad29-c22e-4c05-968f-af868214e888",
            "Kind": "FinalConfirmed",
            "At": "2026-10-17T12:24:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Observed final totals confirmed. Live earned-XP checks were not advanced.",
            "Observation": null
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": {
          "SessionId": "e0960cc5-bd21-4dc8-acfe-424f7f446554",
          "FirstObservedAt": "2026-10-17T12:18:00+00:00",
          "LastObservedAt": "2026-10-17T12:20:00+00:00",
          "ConfirmedAt": "2026-10-17T12:23:00+00:00",
          "Tracks": {
            "Assault": {
              "Id": "5c4184a3-9144-4394-884e-565b2e8f0e6f",
              "Class": "Assault",
              "Xp": 16500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:15:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 16500/1000000XP",
              "Confidence": 1
            }
          },
          "PossiblePlaces": [
            1
          ],
          "PlacementEvidence": [
            {
              "At": "2026-10-17T12:22:00+00:00",
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
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "d3a73d3a-2f6e-49cd-ade2-ab7b63c4b79e",
        "StartedAt": "2026-10-17T12:25:00+00:00",
        "EndedAt": "2026-10-17T12:37:00+00:00",
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
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": true,
            "Baseline": {
              "Id": "924f6abc-77ed-4278-b7a2-01c48847f3ea",
              "Version": 1,
              "Xp": 20000,
              "At": "2026-10-17T12:30:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "924f6abc-77ed-4278-b7a2-01c48847f3ea",
              "Version": 1,
              "Xp": 20000,
              "At": "2026-10-17T12:30:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "a632aeb0-e58b-4d3c-87c3-68abbc891653",
              "Class": "Medic",
              "Xp": 22000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:34:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 22000/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "a632aeb0-e58b-4d3c-87c3-68abbc891653",
              "Class": "Medic",
              "Xp": 22000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:34:00+00:00",
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
            "Id": "10ecc1d0-846f-4fe2-8df9-d3c29320209b",
            "Kind": "SessionStarted",
            "At": "2026-10-17T12:26:00+00:00",
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
            "Id": "02226763-418d-4c46-890e-964130c3561c",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T12:27:00+00:00",
            "Class": "Assault",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Assault tracking disabled.",
            "Observation": null
          },
          {
            "Id": "66f731ac-7f41-4c87-81cb-0a17582df7ed",
            "Kind": "TrackEnabled",
            "At": "2026-10-17T12:28:00+00:00",
            "Class": "Medic",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Medic tracking enabled.",
            "Observation": null
          },
          {
            "Id": "97545cb6-f870-4089-969f-e204f9b362e6",
            "Kind": "DemoScenario",
            "At": "2026-10-17T12:29:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "02 \u00B7 Exact second place: Medic actions 2,000 \u002B bonus 2,800",
            "Observation": null
          },
          {
            "Id": "74d7b068-5674-4af9-b433-7ad011747011",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T12:31:00+00:00",
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
            "Id": "6670dd49-8144-47c7-8764-3ae3889e1cc1",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T12:32:00+00:00",
            "Class": "Medic",
            "Xp": 20000,
            "PreviousXp": null,
            "BaselineId": "924f6abc-77ed-4278-b7a2-01c48847f3ea",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "ed2b814b-c4e7-4a3c-8d16-8d63a56240c9",
              "Class": "Medic",
              "Xp": 20000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:30:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 20000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "8e5df2bb-ecce-4cec-8eeb-33eb8c9cf2c3",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T12:33:00+00:00",
            "Class": "Medic",
            "Xp": 20000,
            "PreviousXp": null,
            "BaselineId": "924f6abc-77ed-4278-b7a2-01c48847f3ea",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "483dafe6-617c-40e4-bbc7-949dfee8354a",
            "Kind": "CurrentXp",
            "At": "2026-10-17T12:35:00+00:00",
            "Class": "Medic",
            "Xp": 22000,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "a632aeb0-e58b-4d3c-87c3-68abbc891653",
              "Class": "Medic",
              "Xp": 22000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:34:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 22000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "81af9ecf-3b95-43b4-9a74-1a595cdc83b7",
            "Kind": "EndDetected",
            "At": "2026-10-17T12:38:00+00:00",
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
            "Id": "ff680644-0da3-4d8c-8729-5ec62782fc9b",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T12:42:00+00:00",
            "Class": "Medic",
            "Xp": 24800,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "7b0e9d98-f54c-4348-a1f4-a471aa602bff",
              "Class": "Medic",
              "Xp": 24800,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:36:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 24800/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "b3d8deb4-21ef-4ed0-ab33-1f759cba77bf",
            "Kind": "FinalConfirmed",
            "At": "2026-10-17T12:45:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Observed final totals confirmed. Live earned-XP checks were not advanced.",
            "Observation": null
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": {
          "SessionId": "d3a73d3a-2f6e-49cd-ade2-ab7b63c4b79e",
          "FirstObservedAt": "2026-10-17T12:39:00+00:00",
          "LastObservedAt": "2026-10-17T12:41:00+00:00",
          "ConfirmedAt": "2026-10-17T12:44:00+00:00",
          "Tracks": {
            "Medic": {
              "Id": "eff4fc43-56d9-4d43-9030-bc052d68fe0a",
              "Class": "Medic",
              "Xp": 24800,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:36:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 24800/1000000XP",
              "Confidence": 1
            }
          },
          "PossiblePlaces": [
            2
          ],
          "PlacementEvidence": [
            {
              "At": "2026-10-17T12:43:00+00:00",
              "Source": "Manual",
              "PossiblePlaces": [
                2
              ],
              "RawText": "User-confirmed placement"
            }
          ],
          "HasConflict": false,
          "ConflictingTracks": [],
          "KeepOldReferencesChosen": false
        },
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "44725e23-077e-467d-928e-a3371d47c0ce",
        "StartedAt": "2026-10-17T12:46:00+00:00",
        "EndedAt": "2026-10-17T12:58:00+00:00",
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
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
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
            "Enabled": true,
            "Baseline": {
              "Id": "04a05705-5028-4905-959b-79f76f26b826",
              "Version": 1,
              "Xp": 30000,
              "At": "2026-10-17T12:51:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "04a05705-5028-4905-959b-79f76f26b826",
              "Version": 1,
              "Xp": 30000,
              "At": "2026-10-17T12:51:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "37f473aa-25f4-43b9-823b-c6dc57600a78",
              "Class": "Driver",
              "Xp": 31500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:55:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 31500/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "37f473aa-25f4-43b9-823b-c6dc57600a78",
              "Class": "Driver",
              "Xp": 31500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:55:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 31500/1000000XP",
              "Confidence": 1
            },
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
            "Id": "36e7b232-02e3-45a1-8f06-2afd97327dc1",
            "Kind": "SessionStarted",
            "At": "2026-10-17T12:47:00+00:00",
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
            "Id": "888a7c13-7e25-40ff-8f4d-60ff93164e60",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T12:48:00+00:00",
            "Class": "Medic",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Medic tracking disabled.",
            "Observation": null
          },
          {
            "Id": "5a8f439d-6f3f-437b-b0c0-eb2656e9820d",
            "Kind": "TrackEnabled",
            "At": "2026-10-17T12:49:00+00:00",
            "Class": "Driver",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Driver tracking enabled.",
            "Observation": null
          },
          {
            "Id": "bfeb07ea-a862-4d52-b183-aa45650191c4",
            "Kind": "DemoScenario",
            "At": "2026-10-17T12:50:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "03 \u00B7 Exact third place: Driver actions 1,500 \u002B bonus 1,800",
            "Observation": null
          },
          {
            "Id": "4cf5d74b-4eaa-4b02-8129-0cdb1106fe3b",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T12:52:00+00:00",
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
            "Id": "392a0dc8-94fc-417d-b735-ade43f821030",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T12:53:00+00:00",
            "Class": "Driver",
            "Xp": 30000,
            "PreviousXp": null,
            "BaselineId": "04a05705-5028-4905-959b-79f76f26b826",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "06aeb43f-124a-4aa4-9ff7-96586c4e0cb2",
              "Class": "Driver",
              "Xp": 30000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:51:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 30000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "74b7c7b6-cc49-4071-bb21-e3b626508e85",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T12:54:00+00:00",
            "Class": "Driver",
            "Xp": 30000,
            "PreviousXp": null,
            "BaselineId": "04a05705-5028-4905-959b-79f76f26b826",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "533daed4-7117-4546-b7d8-66c93db81827",
            "Kind": "CurrentXp",
            "At": "2026-10-17T12:56:00+00:00",
            "Class": "Driver",
            "Xp": 31500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "37f473aa-25f4-43b9-823b-c6dc57600a78",
              "Class": "Driver",
              "Xp": 31500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:55:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 31500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "a588ac48-58a3-4e6f-9e87-6920c14e83f7",
            "Kind": "EndDetected",
            "At": "2026-10-17T12:59:00+00:00",
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
            "Id": "41dfb2a3-9d8d-4310-8f95-fe97b67e2bf9",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T13:03:00+00:00",
            "Class": "Driver",
            "Xp": 33300,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "0b72aba2-e20d-4608-b787-0caac338e819",
              "Class": "Driver",
              "Xp": 33300,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:57:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 33300/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "3b137410-770a-4251-b020-c6f75dafde29",
            "Kind": "FinalConfirmed",
            "At": "2026-10-17T13:06:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Observed final totals confirmed. Live earned-XP checks were not advanced.",
            "Observation": null
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": {
          "SessionId": "44725e23-077e-467d-928e-a3371d47c0ce",
          "FirstObservedAt": "2026-10-17T13:00:00+00:00",
          "LastObservedAt": "2026-10-17T13:02:00+00:00",
          "ConfirmedAt": "2026-10-17T13:05:00+00:00",
          "Tracks": {
            "Driver": {
              "Id": "83a4b06e-2eab-44d8-a745-00f555844964",
              "Class": "Driver",
              "Xp": 33300,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T12:57:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 33300/1000000XP",
              "Confidence": 1
            }
          },
          "PossiblePlaces": [
            3
          ],
          "PlacementEvidence": [
            {
              "At": "2026-10-17T13:04:00+00:00",
              "Source": "Manual",
              "PossiblePlaces": [
                3
              ],
              "RawText": "User-confirmed placement"
            }
          ],
          "HasConflict": false,
          "ConflictingTracks": [],
          "KeepOldReferencesChosen": false
        },
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "46b845db-9c70-456c-b674-eb459cee1606",
        "StartedAt": "2026-10-17T13:07:00+00:00",
        "EndedAt": "2026-10-17T13:19:00+00:00",
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
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
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
            "Enabled": true,
            "Baseline": {
              "Id": "32166e47-3150-441d-8a10-df26ab88a579",
              "Version": 1,
              "Xp": 40000,
              "At": "2026-10-17T13:12:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "32166e47-3150-441d-8a10-df26ab88a579",
              "Version": 1,
              "Xp": 40000,
              "At": "2026-10-17T13:12:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "92b771ab-123d-4abf-8e49-bb01b400e07b",
              "Class": "Pilot",
              "Xp": 42500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:16:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 42500/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "92b771ab-123d-4abf-8e49-bb01b400e07b",
              "Class": "Pilot",
              "Xp": 42500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:16:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 42500/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": null,
            "TargetReachedAt": "2026-10-17T13:16:00+00:00",
            "WasRebased": false
          }
        },
        "History": [
          {
            "Id": "589595a5-5390-4498-b40f-642351a02bc3",
            "Kind": "SessionStarted",
            "At": "2026-10-17T13:08:00+00:00",
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
            "Id": "ca8d53fd-c799-4aee-a349-fae76117f398",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T13:09:00+00:00",
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
            "Id": "f4baa7f4-b2e1-4dcc-941c-fe5b414744f2",
            "Kind": "TrackEnabled",
            "At": "2026-10-17T13:10:00+00:00",
            "Class": "Pilot",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Pilot tracking enabled.",
            "Observation": null
          },
          {
            "Id": "7530e787-1c13-4a00-8a0f-b3a0e7036a2b",
            "Kind": "DemoScenario",
            "At": "2026-10-17T13:11:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "04 \u00B7 DEFEAT: Pilot total 6,500, 500 XP indeterminate",
            "Observation": null
          },
          {
            "Id": "3c0ae1db-c3a6-4d87-9505-bb7472aa3911",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T13:13:00+00:00",
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
            "Id": "9b4ab15f-081d-4e77-9afa-b63f4d7cd65f",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T13:14:00+00:00",
            "Class": "Pilot",
            "Xp": 40000,
            "PreviousXp": null,
            "BaselineId": "32166e47-3150-441d-8a10-df26ab88a579",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "a1d122e3-974e-422c-8ebb-83fb6fb93908",
              "Class": "Pilot",
              "Xp": 40000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:12:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 40000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "429ed8b2-fb6d-4709-9a73-67975211f051",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T13:15:00+00:00",
            "Class": "Pilot",
            "Xp": 40000,
            "PreviousXp": null,
            "BaselineId": "32166e47-3150-441d-8a10-df26ab88a579",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "d14cb26d-58b1-4edf-b743-54130ab61194",
            "Kind": "CurrentXp",
            "At": "2026-10-17T13:17:00+00:00",
            "Class": "Pilot",
            "Xp": 42500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "92b771ab-123d-4abf-8e49-bb01b400e07b",
              "Class": "Pilot",
              "Xp": 42500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:16:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 42500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "3a47a8e6-1030-4856-9d6e-5ce0b2675ad4",
            "Kind": "MaximumTierReached",
            "At": "2026-10-17T13:18:00+00:00",
            "Class": "Pilot",
            "Xp": 42500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Observed earned XP reached the maximum bonus tier. Bonus receipt is separate.",
            "Observation": {
              "Id": "92b771ab-123d-4abf-8e49-bb01b400e07b",
              "Class": "Pilot",
              "Xp": 42500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:16:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 42500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "a866695d-202f-4f74-b0db-da1644bfa735",
            "Kind": "EndDetected",
            "At": "2026-10-17T13:20:00+00:00",
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
            "Id": "e59cfa97-954e-493e-bbaa-5534c2d16b49",
            "Kind": "PlacementObserved",
            "At": "2026-10-17T13:24:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndScore",
            "Message": "MOCK DEFEAT",
            "Observation": null
          },
          {
            "Id": "ce7177e9-1bad-437c-9d0b-d6f399e2ed32",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T13:27:00+00:00",
            "Class": "Pilot",
            "Xp": 46500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "2c0f6c41-535f-4433-9950-ef979a8d2112",
              "Class": "Pilot",
              "Xp": 46500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:25:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 46500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "457d25a0-4833-4506-9677-46d2a30c8a62",
            "Kind": "FinalConfirmed",
            "At": "2026-10-17T13:29:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Observed final totals confirmed. Live earned-XP checks were not advanced.",
            "Observation": null
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": {
          "SessionId": "46b845db-9c70-456c-b674-eb459cee1606",
          "FirstObservedAt": "2026-10-17T13:21:00+00:00",
          "LastObservedAt": "2026-10-17T13:26:00+00:00",
          "ConfirmedAt": "2026-10-17T13:28:00+00:00",
          "Tracks": {
            "Pilot": {
              "Id": "15157b5c-f5d5-43f1-bb19-db25f58a728f",
              "Class": "Pilot",
              "Xp": 46500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:25:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 46500/1000000XP",
              "Confidence": 1
            }
          },
          "PossiblePlaces": [
            2,
            3
          ],
          "PlacementEvidence": [
            {
              "At": "2026-10-17T13:23:00+00:00",
              "Source": "AutomaticOcr",
              "PossiblePlaces": [
                2,
                3
              ],
              "RawText": "MOCK DEFEAT"
            }
          ],
          "HasConflict": false,
          "ConflictingTracks": [],
          "KeepOldReferencesChosen": false
        },
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "6c42e541-d23b-4378-af79-471425f682fe",
        "StartedAt": "2026-10-17T13:30:00+00:00",
        "EndedAt": "2026-10-17T13:43:00+00:00",
        "State": "Ended",
        "Outcome": "Unknown",
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
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Recon": {
            "Class": "Recon",
            "Enabled": true,
            "Baseline": {
              "Id": "e41cffd3-a88b-4ce0-8cf1-e1cd2febeb12",
              "Version": 1,
              "Xp": 50000,
              "At": "2026-10-17T13:35:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "e41cffd3-a88b-4ce0-8cf1-e1cd2febeb12",
              "Version": 1,
              "Xp": 50000,
              "At": "2026-10-17T13:35:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "63a9cad9-6843-42e8-9981-976a08d33a47",
              "Class": "Recon",
              "Xp": 52500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:39:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 52500/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "63a9cad9-6843-42e8-9981-976a08d33a47",
              "Class": "Recon",
              "Xp": 52500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:39:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 52500/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": null,
            "TargetReachedAt": "2026-10-17T13:39:00+00:00",
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
            "Id": "6a2d2578-1a9f-4f58-b66f-f97b0299f4f9",
            "Kind": "SessionStarted",
            "At": "2026-10-17T13:31:00+00:00",
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
            "Id": "8594a4a4-5248-4a39-bdd9-d25de82ed748",
            "Kind": "TrackEnabled",
            "At": "2026-10-17T13:32:00+00:00",
            "Class": "Recon",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Recon tracking enabled.",
            "Observation": null
          },
          {
            "Id": "16004290-ffa9-4f08-9dcf-7d2f295c815d",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T13:33:00+00:00",
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
            "Id": "f85de4cb-470d-45b7-bac6-3e580f1e6bfb",
            "Kind": "DemoScenario",
            "At": "2026-10-17T13:34:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "05 \u00B7 Confirmed Recon total 6,500 with completion unknown",
            "Observation": null
          },
          {
            "Id": "e7e5d3f5-2d72-491f-825b-182cf0d2e948",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T13:36:00+00:00",
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
            "Id": "be2acc69-66bc-4128-921e-2f1f11e5bb8a",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T13:37:00+00:00",
            "Class": "Recon",
            "Xp": 50000,
            "PreviousXp": null,
            "BaselineId": "e41cffd3-a88b-4ce0-8cf1-e1cd2febeb12",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "5c56d18b-d945-4d63-94c9-e5389431cfbf",
              "Class": "Recon",
              "Xp": 50000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:35:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 50000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "d6b78b85-f064-4074-a3e7-5717f94c5b61",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T13:38:00+00:00",
            "Class": "Recon",
            "Xp": 50000,
            "PreviousXp": null,
            "BaselineId": "e41cffd3-a88b-4ce0-8cf1-e1cd2febeb12",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "aa706261-b4aa-4be6-9706-cbd4dc46509e",
            "Kind": "CurrentXp",
            "At": "2026-10-17T13:40:00+00:00",
            "Class": "Recon",
            "Xp": 52500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "63a9cad9-6843-42e8-9981-976a08d33a47",
              "Class": "Recon",
              "Xp": 52500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:39:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 52500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "0aaefbc1-d6d1-497e-b42e-267f5bedae25",
            "Kind": "MaximumTierReached",
            "At": "2026-10-17T13:41:00+00:00",
            "Class": "Recon",
            "Xp": 52500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Observed earned XP reached the maximum bonus tier. Bonus receipt is separate.",
            "Observation": {
              "Id": "63a9cad9-6843-42e8-9981-976a08d33a47",
              "Class": "Recon",
              "Xp": 52500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:39:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 52500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "8c653457-3fc7-4755-b9ca-f0c8dd8fe895",
            "Kind": "EndDetected",
            "At": "2026-10-17T13:44:00+00:00",
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
            "Id": "b8f91a11-217d-4630-9718-f977c4583729",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T13:48:00+00:00",
            "Class": "Recon",
            "Xp": 56500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "160904e5-d881-471e-b817-b5c2cb26a883",
              "Class": "Recon",
              "Xp": 56500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:42:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 56500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "a98b4e91-ec0c-4477-a2c5-7f5fd7f33da2",
            "Kind": "FinalConfirmed",
            "At": "2026-10-17T13:50:00+00:00",
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
            "Id": "eac04155-0cb6-46b4-80c1-262cdca5db2b",
            "Kind": "SessionEnded",
            "At": "2026-10-17T13:51:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Session ended: Unknown.",
            "Observation": null
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": {
          "SessionId": "6c42e541-d23b-4378-af79-471425f682fe",
          "FirstObservedAt": "2026-10-17T13:45:00+00:00",
          "LastObservedAt": "2026-10-17T13:47:00+00:00",
          "ConfirmedAt": "2026-10-17T13:49:00+00:00",
          "Tracks": {
            "Recon": {
              "Id": "0ff51e86-7ebd-4eaa-857d-e66df6758fcf",
              "Class": "Recon",
              "Xp": 56500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:42:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 56500/1000000XP",
              "Confidence": 1
            }
          },
          "PossiblePlaces": [],
          "PlacementEvidence": [],
          "HasConflict": false,
          "ConflictingTracks": [],
          "KeepOldReferencesChosen": false
        },
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "7a043070-c163-4f8f-9b73-96be8be93477",
        "StartedAt": "2026-10-17T13:52:00+00:00",
        "EndedAt": "2026-10-17T14:02:00+00:00",
        "State": "Ended",
        "Outcome": "LeftMatch",
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
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Recon": {
            "Class": "Recon",
            "Enabled": true,
            "Baseline": {
              "Id": "c96cb0c9-c914-42a2-9f17-3ec94301c9ed",
              "Version": 1,
              "Xp": 60000,
              "At": "2026-10-17T13:55:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "c96cb0c9-c914-42a2-9f17-3ec94301c9ed",
              "Version": 1,
              "Xp": 60000,
              "At": "2026-10-17T13:55:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "d0b0700c-9d8d-4054-8cdf-87c21dd35df7",
              "Class": "Recon",
              "Xp": 63000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:59:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 63000/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "d0b0700c-9d8d-4054-8cdf-87c21dd35df7",
              "Class": "Recon",
              "Xp": 63000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:59:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 63000/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": {
              "Id": "16885575-d4ed-446c-a8c0-97e456de25a9",
              "Class": "Recon",
              "Xp": 63000,
              "NextThreshold": null,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:04:00+00:00",
              "Source": "Manual",
              "Screen": "Unlock",
              "RawText": "Manual entry",
              "Confidence": null
            },
            "TargetReachedAt": "2026-10-17T13:59:00+00:00",
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
            "Id": "60964719-f534-466f-b47d-2f96c536156c",
            "Kind": "SessionStarted",
            "At": "2026-10-17T13:53:00+00:00",
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
            "Id": "5b5a8ab5-8f92-4dc8-bb09-7de391c3569b",
            "Kind": "DemoScenario",
            "At": "2026-10-17T13:54:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "06 \u00B7 Left match: Recon actions 3,000, average missed bonus 3,500",
            "Observation": null
          },
          {
            "Id": "09aa8dff-824f-4557-99a7-fc1c13d8dd98",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T13:56:00+00:00",
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
            "Id": "2338ac6d-e061-42d3-9fef-0cbbb979c11f",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T13:57:00+00:00",
            "Class": "Recon",
            "Xp": 60000,
            "PreviousXp": null,
            "BaselineId": "c96cb0c9-c914-42a2-9f17-3ec94301c9ed",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "ec1ca799-3c12-4ad0-a3aa-c8a2e73c8ce5",
              "Class": "Recon",
              "Xp": 60000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:55:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 60000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "231f0be2-3fd8-4af8-b89b-cf1722279cb0",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T13:58:00+00:00",
            "Class": "Recon",
            "Xp": 60000,
            "PreviousXp": null,
            "BaselineId": "c96cb0c9-c914-42a2-9f17-3ec94301c9ed",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "fcb8bcae-631e-4582-b56a-35f6b932bdb8",
            "Kind": "CurrentXp",
            "At": "2026-10-17T14:00:00+00:00",
            "Class": "Recon",
            "Xp": 63000,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "d0b0700c-9d8d-4054-8cdf-87c21dd35df7",
              "Class": "Recon",
              "Xp": 63000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:59:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 63000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "77f1bec7-e434-4f87-9ca0-137faf07f8b9",
            "Kind": "MaximumTierReached",
            "At": "2026-10-17T14:01:00+00:00",
            "Class": "Recon",
            "Xp": 63000,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Observed earned XP reached the maximum bonus tier. Bonus receipt is separate.",
            "Observation": {
              "Id": "d0b0700c-9d8d-4054-8cdf-87c21dd35df7",
              "Class": "Recon",
              "Xp": 63000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T13:59:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 63000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "2084aa51-90e8-4214-b191-27fe797f8781",
            "Kind": "SessionEnded",
            "At": "2026-10-17T14:03:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "User reported no bonus received. Closing XP can be recorded separately.",
            "Observation": null
          },
          {
            "Id": "dcccf856-5a06-41d6-b6fb-166e8d703776",
            "Kind": "ClosingXp",
            "At": "2026-10-17T14:05:00+00:00",
            "Class": "Recon",
            "Xp": 63000,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": "Unlock",
            "Message": "Closing XP recorded separately from the live counter and baseline.",
            "Observation": {
              "Id": "16885575-d4ed-446c-a8c0-97e456de25a9",
              "Class": "Recon",
              "Xp": 63000,
              "NextThreshold": null,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:04:00+00:00",
              "Source": "Manual",
              "Screen": "Unlock",
              "RawText": "Manual entry",
              "Confidence": null
            }
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": null,
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "610e2c56-542c-4700-8a2f-27ed524e6e51",
        "StartedAt": "2026-10-17T14:06:00+00:00",
        "EndedAt": "2026-10-17T14:17:00+00:00",
        "State": "Ended",
        "Outcome": "Disconnected",
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
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
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
            "Enabled": true,
            "Baseline": {
              "Id": "5a52f3d7-7d89-474f-aa11-8f81f3cabdb7",
              "Version": 1,
              "Xp": 70000,
              "At": "2026-10-17T14:11:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "5a52f3d7-7d89-474f-aa11-8f81f3cabdb7",
              "Version": 1,
              "Xp": 70000,
              "At": "2026-10-17T14:11:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "219c83ff-8e9a-4e02-9b2a-13fa77be11a6",
              "Class": "Support",
              "Xp": 71200,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:15:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 71200/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "219c83ff-8e9a-4e02-9b2a-13fa77be11a6",
              "Class": "Support",
              "Xp": 71200,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:15:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 71200/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": {
              "Id": "6e5e283b-e406-4ff5-91ed-d35e837dfa37",
              "Class": "Support",
              "Xp": 71200,
              "NextThreshold": null,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:19:00+00:00",
              "Source": "Manual",
              "Screen": "Unlock",
              "RawText": "Manual entry",
              "Confidence": null
            },
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
            "Id": "3e43d1d1-21af-4ed7-bf47-bdad60390c2e",
            "Kind": "SessionStarted",
            "At": "2026-10-17T14:07:00+00:00",
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
            "Id": "5e06fb40-a409-48a1-b0cb-47e6d0a37e12",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T14:08:00+00:00",
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
            "Id": "ff00da23-81df-4dff-b001-cc925d83e3d3",
            "Kind": "TrackEnabled",
            "At": "2026-10-17T14:09:00+00:00",
            "Class": "Support",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Support tracking enabled.",
            "Observation": null
          },
          {
            "Id": "9b4a257b-face-491b-ac7b-d59683b36187",
            "Kind": "DemoScenario",
            "At": "2026-10-17T14:10:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "07 \u00B7 Disconnected: Support actions 1,200, average missed bonus 1,400",
            "Observation": null
          },
          {
            "Id": "9dcc0e2c-1e0d-4b06-aa07-391bef1272e5",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T14:12:00+00:00",
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
            "Id": "1305d974-5757-421f-a0ba-85b0009b26d6",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T14:13:00+00:00",
            "Class": "Support",
            "Xp": 70000,
            "PreviousXp": null,
            "BaselineId": "5a52f3d7-7d89-474f-aa11-8f81f3cabdb7",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "6f69a6bc-b13e-4b48-8122-6181e5f6557d",
              "Class": "Support",
              "Xp": 70000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:11:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 70000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "544e6639-c1ef-4ce9-9b9f-4dca942aaf63",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T14:14:00+00:00",
            "Class": "Support",
            "Xp": 70000,
            "PreviousXp": null,
            "BaselineId": "5a52f3d7-7d89-474f-aa11-8f81f3cabdb7",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "2498ecf7-0116-428e-87da-c9706f9293fe",
            "Kind": "CurrentXp",
            "At": "2026-10-17T14:16:00+00:00",
            "Class": "Support",
            "Xp": 71200,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "219c83ff-8e9a-4e02-9b2a-13fa77be11a6",
              "Class": "Support",
              "Xp": 71200,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:15:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 71200/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "5566c16a-6087-4142-b5a8-a793e73e9b36",
            "Kind": "SessionEnded",
            "At": "2026-10-17T14:18:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "User reported no bonus received. Closing XP can be recorded separately.",
            "Observation": null
          },
          {
            "Id": "4ce5a07a-66be-4264-915f-adde50defd53",
            "Kind": "ClosingXp",
            "At": "2026-10-17T14:20:00+00:00",
            "Class": "Support",
            "Xp": 71200,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": "Unlock",
            "Message": "Closing XP recorded separately from the live counter and baseline.",
            "Observation": {
              "Id": "6e5e283b-e406-4ff5-91ed-d35e837dfa37",
              "Class": "Support",
              "Xp": 71200,
              "NextThreshold": null,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:19:00+00:00",
              "Source": "Manual",
              "Screen": "Unlock",
              "RawText": "Manual entry",
              "Confidence": null
            }
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": null,
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "fe186b6b-7127-4bfe-9189-5d72a7bc2522",
        "StartedAt": "2026-10-17T14:21:00+00:00",
        "EndedAt": "2026-10-17T14:31:00+00:00",
        "State": "Ended",
        "Outcome": "LeftMatch",
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
              "Id": "01abd133-b118-4bc2-9e7c-333d24fd4c31",
              "Version": 1,
              "Xp": 80000,
              "At": "2026-10-17T14:26:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "Unknown",
              "Validity": "Unknown",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "01abd133-b118-4bc2-9e7c-333d24fd4c31",
              "Version": 1,
              "Xp": 80000,
              "At": "2026-10-17T14:26:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "Unknown",
              "Validity": "Unknown",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "d5f77d78-3244-4664-b5fd-8b3ef6b64f29",
              "Class": "Assault",
              "Xp": 81500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:29:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 81500/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "d5f77d78-3244-4664-b5fd-8b3ef6b64f29",
              "Class": "Assault",
              "Xp": 81500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:29:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 81500/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
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
            "Id": "23941082-2419-44a3-aae8-60826a65e86f",
            "Kind": "SessionStarted",
            "At": "2026-10-17T14:22:00+00:00",
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
            "Id": "c351c2e2-069b-4eb0-b6c2-ed1462692f68",
            "Kind": "TrackEnabled",
            "At": "2026-10-17T14:23:00+00:00",
            "Class": "Assault",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Assault tracking enabled.",
            "Observation": null
          },
          {
            "Id": "10700773-ef80-4308-80d0-eef341193610",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T14:24:00+00:00",
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
            "Id": "5f1ad1a7-5e17-40b7-815b-ab75b74edd17",
            "Kind": "DemoScenario",
            "At": "2026-10-17T14:25:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "08 \u00B7 Partial early exit: Assault observed interval 1,500, missed bonus lower bound",
            "Observation": null
          },
          {
            "Id": "8297ffb5-15e5-4ea9-9bf1-cdc7398e697b",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T14:27:00+00:00",
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
            "Id": "b354fa02-2123-4776-800a-7aeed5519408",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T14:28:00+00:00",
            "Class": "Assault",
            "Xp": 80000,
            "PreviousXp": null,
            "BaselineId": "01abd133-b118-4bc2-9e7c-333d24fd4c31",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "9346c949-9853-495b-8880-79b37975fd40",
              "Class": "Assault",
              "Xp": 80000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:26:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 80000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "68b2eacb-daa9-4c7a-aac7-d1c0e8b5feca",
            "Kind": "CurrentXp",
            "At": "2026-10-17T14:30:00+00:00",
            "Class": "Assault",
            "Xp": 81500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "d5f77d78-3244-4664-b5fd-8b3ef6b64f29",
              "Class": "Assault",
              "Xp": 81500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:29:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 81500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "58514ffa-0cd6-4bdb-a007-4dd706f966ad",
            "Kind": "SessionEnded",
            "At": "2026-10-17T14:32:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "User reported no bonus received. Closing XP can be recorded separately.",
            "Observation": null
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": null,
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "f7759c67-257d-400a-ad8a-0d5fefa86bd5",
        "StartedAt": "2026-10-17T14:33:00+00:00",
        "EndedAt": "2026-10-17T14:46:00+00:00",
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
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": true,
            "Baseline": {
              "Id": "fb55e29c-5c4f-4695-86dd-1ff4ef0fd3bd",
              "Version": 1,
              "Xp": 90000,
              "At": "2026-10-17T14:38:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "fb55e29c-5c4f-4695-86dd-1ff4ef0fd3bd",
              "Version": 1,
              "Xp": 90000,
              "At": "2026-10-17T14:38:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "bcd79e81-e109-4cf7-afa2-44dcafae82c6",
              "Class": "Medic",
              "Xp": 92500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:42:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 92500/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "bcd79e81-e109-4cf7-afa2-44dcafae82c6",
              "Class": "Medic",
              "Xp": 92500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:42:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 92500/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": null,
            "TargetReachedAt": "2026-10-17T14:42:00+00:00",
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
            "Id": "23572192-8b7f-4a56-9834-1e7de120b07a",
            "Kind": "SessionStarted",
            "At": "2026-10-17T14:34:00+00:00",
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
            "Id": "20a1dcca-db5a-487c-82d3-4cf34c540970",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T14:35:00+00:00",
            "Class": "Assault",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Assault tracking disabled.",
            "Observation": null
          },
          {
            "Id": "c2dd1acd-e67d-4cbe-b9fb-dc0e3d2896c5",
            "Kind": "TrackEnabled",
            "At": "2026-10-17T14:36:00+00:00",
            "Class": "Medic",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Medic tracking enabled.",
            "Observation": null
          },
          {
            "Id": "ec4295c9-2a8b-4eff-84e5-2e6a4fe9f1ad",
            "Kind": "DemoScenario",
            "At": "2026-10-17T14:37:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "09 \u00B7 Rules mismatch: Medic final delta 2,501 contradicts observed tier-five gain",
            "Observation": null
          },
          {
            "Id": "f1cba9c3-2885-497b-931e-7ebc6ae07997",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T14:39:00+00:00",
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
            "Id": "92979ca9-21c6-4275-bab4-c0fa50bbcb5f",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T14:40:00+00:00",
            "Class": "Medic",
            "Xp": 90000,
            "PreviousXp": null,
            "BaselineId": "fb55e29c-5c4f-4695-86dd-1ff4ef0fd3bd",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "45923386-a0bb-41dd-b903-e00fdd6a17de",
              "Class": "Medic",
              "Xp": 90000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:38:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 90000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "7398703d-e9f8-4acc-8b7c-a0804e4336ec",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T14:41:00+00:00",
            "Class": "Medic",
            "Xp": 90000,
            "PreviousXp": null,
            "BaselineId": "fb55e29c-5c4f-4695-86dd-1ff4ef0fd3bd",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "970d13af-bf8e-460f-8a3b-9530efc4e1e4",
            "Kind": "CurrentXp",
            "At": "2026-10-17T14:43:00+00:00",
            "Class": "Medic",
            "Xp": 92500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "bcd79e81-e109-4cf7-afa2-44dcafae82c6",
              "Class": "Medic",
              "Xp": 92500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:42:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 92500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "f2f9447f-25df-48b8-94c8-26343822d40a",
            "Kind": "MaximumTierReached",
            "At": "2026-10-17T14:44:00+00:00",
            "Class": "Medic",
            "Xp": 92500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Observed earned XP reached the maximum bonus tier. Bonus receipt is separate.",
            "Observation": {
              "Id": "bcd79e81-e109-4cf7-afa2-44dcafae82c6",
              "Class": "Medic",
              "Xp": 92500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:42:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 92500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "d5520c7d-68fd-4df8-bb48-e0bb83e45589",
            "Kind": "EndDetected",
            "At": "2026-10-17T14:47:00+00:00",
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
            "Id": "d79837f4-11ab-4ba4-a8bf-36ccc445a416",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T14:51:00+00:00",
            "Class": "Medic",
            "Xp": 92501,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "77783091-4368-4253-a033-af455e1a4d9d",
              "Class": "Medic",
              "Xp": 92501,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:45:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 92501/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "2148a6eb-0c6d-4d94-ba5e-4a24aecc7c95",
            "Kind": "FinalConfirmed",
            "At": "2026-10-17T14:54:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Observed final totals confirmed. Live earned-XP checks were not advanced.",
            "Observation": null
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": {
          "SessionId": "f7759c67-257d-400a-ad8a-0d5fefa86bd5",
          "FirstObservedAt": "2026-10-17T14:48:00+00:00",
          "LastObservedAt": "2026-10-17T14:50:00+00:00",
          "ConfirmedAt": "2026-10-17T14:53:00+00:00",
          "Tracks": {
            "Medic": {
              "Id": "55de6657-8817-4ec5-b920-a5d960ec0ebb",
              "Class": "Medic",
              "Xp": 92501,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T14:45:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 92501/1000000XP",
              "Confidence": 1
            }
          },
          "PossiblePlaces": [
            1
          ],
          "PlacementEvidence": [
            {
              "At": "2026-10-17T14:52:00+00:00",
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
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "1b7bb638-8a41-4c38-a374-16d5b7112b9c",
        "StartedAt": "2026-10-17T14:55:00+00:00",
        "EndedAt": "2026-10-17T15:08:00+00:00",
        "State": "Frozen",
        "Outcome": "Unknown",
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
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
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
            "Enabled": true,
            "Baseline": {
              "Id": "0f95def0-b08b-44e5-8864-3169ece8d3b2",
              "Version": 1,
              "Xp": 100000,
              "At": "2026-10-17T15:00:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "0f95def0-b08b-44e5-8864-3169ece8d3b2",
              "Version": 1,
              "Xp": 100000,
              "At": "2026-10-17T15:00:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "8d5870cc-b82c-4e87-b1d5-88326cf922bc",
              "Class": "Driver",
              "Xp": 102500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:04:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 102500/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "8d5870cc-b82c-4e87-b1d5-88326cf922bc",
              "Class": "Driver",
              "Xp": 102500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:04:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 102500/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": null,
            "TargetReachedAt": "2026-10-17T15:04:00+00:00",
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
            "Id": "4aa5ac7e-cea8-477f-96ef-a2b841ee068a",
            "Kind": "SessionStarted",
            "At": "2026-10-17T14:56:00+00:00",
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
            "Id": "cdafc014-b172-49be-9943-e2a093bbe867",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T14:57:00+00:00",
            "Class": "Medic",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Medic tracking disabled.",
            "Observation": null
          },
          {
            "Id": "82d3af44-7c36-42a8-82cc-916d963e2933",
            "Kind": "TrackEnabled",
            "At": "2026-10-17T14:58:00+00:00",
            "Class": "Driver",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Driver tracking enabled.",
            "Observation": null
          },
          {
            "Id": "8b96fa26-ef16-4325-94af-7b6df5f6753e",
            "Kind": "DemoScenario",
            "At": "2026-10-17T14:59:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "10 \u00B7 Unconfirmed Driver final candidate is excluded from received totals",
            "Observation": null
          },
          {
            "Id": "70335fb4-f52d-486a-a895-a5f840e7ab64",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T15:01:00+00:00",
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
            "Id": "10f1f840-875a-4be5-b550-7511973ec7b8",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T15:02:00+00:00",
            "Class": "Driver",
            "Xp": 100000,
            "PreviousXp": null,
            "BaselineId": "0f95def0-b08b-44e5-8864-3169ece8d3b2",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "e7556f7d-4ed5-4bb7-b5bf-94dc804caf83",
              "Class": "Driver",
              "Xp": 100000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:00:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 100000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "d450168d-45c6-4123-8155-85cae619295e",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T15:03:00+00:00",
            "Class": "Driver",
            "Xp": 100000,
            "PreviousXp": null,
            "BaselineId": "0f95def0-b08b-44e5-8864-3169ece8d3b2",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "07419ad7-818b-433a-a25b-18b61678287e",
            "Kind": "CurrentXp",
            "At": "2026-10-17T15:05:00+00:00",
            "Class": "Driver",
            "Xp": 102500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "8d5870cc-b82c-4e87-b1d5-88326cf922bc",
              "Class": "Driver",
              "Xp": 102500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:04:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 102500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "8d197378-a5bb-4030-aa36-a69eefd02c2a",
            "Kind": "MaximumTierReached",
            "At": "2026-10-17T15:06:00+00:00",
            "Class": "Driver",
            "Xp": 102500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Observed earned XP reached the maximum bonus tier. Bonus receipt is separate.",
            "Observation": {
              "Id": "8d5870cc-b82c-4e87-b1d5-88326cf922bc",
              "Class": "Driver",
              "Xp": 102500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:04:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 102500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "706619e3-5b5b-43f4-9b68-66d4610b6ea0",
            "Kind": "EndDetected",
            "At": "2026-10-17T15:09:00+00:00",
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
            "Id": "62cd40f9-5821-49a9-9c54-3ed06e014330",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T15:13:00+00:00",
            "Class": "Driver",
            "Xp": 106500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "8769bc63-e20a-461b-ba3b-a88a6b1e048e",
              "Class": "Driver",
              "Xp": 106500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:07:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 106500/1000000XP",
              "Confidence": 1
            }
          }
        ],
        "PendingFinal": {
          "SessionId": "1b7bb638-8a41-4c38-a374-16d5b7112b9c",
          "FirstObservedAt": "2026-10-17T15:10:00+00:00",
          "LastObservedAt": "2026-10-17T15:12:00+00:00",
          "ConfirmedAt": null,
          "Tracks": {
            "Driver": {
              "Id": "8769bc63-e20a-461b-ba3b-a88a6b1e048e",
              "Class": "Driver",
              "Xp": 106500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:07:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 106500/1000000XP",
              "Confidence": 1
            }
          },
          "PossiblePlaces": [],
          "PlacementEvidence": [],
          "HasConflict": false,
          "ConflictingTracks": [],
          "KeepOldReferencesChosen": false
        },
        "ConfirmedFinal": null,
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      },
      {
        "Id": "1968524d-cb13-4a41-a2cc-1a81bcc59afc",
        "StartedAt": "2026-10-17T15:14:00+00:00",
        "EndedAt": "2026-10-17T15:31:00+00:00",
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
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": false
          },
          "Medic": {
            "Class": "Medic",
            "Enabled": false,
            "Baseline": null,
            "OriginalBaseline": null,
            "Current": null,
            "LatestReading": null,
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
            "Enabled": true,
            "Baseline": {
              "Id": "c8574f78-0c53-4e81-a3e2-d8194eb7c212",
              "Version": 2,
              "Xp": 112000,
              "At": "2026-10-17T15:26:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "Partial",
              "Validity": "Unknown",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "OriginalBaseline": {
              "Id": "e07dd518-9535-4aaa-8509-8fe63c02f112",
              "Version": 1,
              "Xp": 110000,
              "At": "2026-10-17T15:19:00+00:00",
              "Origin": "SnapshotHotkey",
              "Coverage": "BeforeEarnedXp",
              "Validity": "Valid",
              "ValidityReason": null,
              "RelatedSessionId": null
            },
            "Current": {
              "Id": "2c57a6c9-5b75-4b77-81e1-3efdf7594d9e",
              "Class": "Support",
              "Xp": 113000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:28:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 113000/1000000XP",
              "Confidence": 1
            },
            "LatestReading": {
              "Id": "2c57a6c9-5b75-4b77-81e1-3efdf7594d9e",
              "Class": "Support",
              "Xp": 113000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:28:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 113000/1000000XP",
              "Confidence": 1
            },
            "ClosingXp": null,
            "TargetReachedAt": null,
            "WasRebased": true
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
            "Id": "61dfafd2-e0de-489e-a8dd-c83a54a71f65",
            "Kind": "SessionStarted",
            "At": "2026-10-17T15:15:00+00:00",
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
            "Id": "7311c199-600a-45e6-8664-4680b89c7a74",
            "Kind": "TrackEnabled",
            "At": "2026-10-17T15:16:00+00:00",
            "Class": "Support",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Support tracking enabled.",
            "Observation": null
          },
          {
            "Id": "2bad9921-9a6a-4a0a-90fe-e55270a942d6",
            "Kind": "TrackDisabled",
            "At": "2026-10-17T15:17:00+00:00",
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
            "Id": "6ebb67ef-2d88-46b3-b582-72fa516fe917",
            "Kind": "DemoScenario",
            "At": "2026-10-17T15:18:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "Manual",
            "Screen": null,
            "Message": "11 \u00B7 Support rebase preserves a certified original: actions 3,000 \u002B bonus 3,500",
            "Observation": null
          },
          {
            "Id": "cd61d07f-d763-4091-bab3-17499fa98a52",
            "Kind": "UnlockObserved",
            "At": "2026-10-17T15:20:00+00:00",
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
            "Id": "bd8410ef-0477-4bfe-a55a-b9a534c7e6d0",
            "Kind": "BaselineEstablished",
            "At": "2026-10-17T15:21:00+00:00",
            "Class": "Support",
            "Xp": 110000,
            "PreviousXp": null,
            "BaselineId": "e07dd518-9535-4aaa-8509-8fe63c02f112",
            "BaselineVersion": 1,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "Baseline established.",
            "Observation": {
              "Id": "b6bd46ac-1e28-4316-9ce9-be2030ee7a52",
              "Class": "Support",
              "Xp": 110000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:19:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 110000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "5b0f0569-2675-4199-a62c-739d5bbd2d02",
            "Kind": "CoverageConfirmed",
            "At": "2026-10-17T15:22:00+00:00",
            "Class": "Support",
            "Xp": 110000,
            "PreviousXp": null,
            "BaselineId": "e07dd518-9535-4aaa-8509-8fe63c02f112",
            "BaselineVersion": 1,
            "Source": "Manual",
            "Screen": null,
            "Message": "User confirmed this baseline preceded earned XP.",
            "Observation": null
          },
          {
            "Id": "3162820b-99ed-4af0-915f-7aa8fd5a17e5",
            "Kind": "CurrentXp",
            "At": "2026-10-17T15:24:00+00:00",
            "Class": "Support",
            "Xp": 112000,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "20e5c3ae-fe64-4bb2-a687-1f41754ec046",
              "Class": "Support",
              "Xp": 112000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:23:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 112000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "2271e57e-9030-40bd-98e6-f4ab5196a935",
            "Kind": "RebaseArmed",
            "At": "2026-10-17T15:25:00+00:00",
            "Class": "Support",
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "The previous baseline stays active until a valid replacement is entered.",
            "Observation": null
          },
          {
            "Id": "03cb6284-4737-453c-b375-8e69ab087fed",
            "Kind": "BaselineRebased",
            "At": "2026-10-17T15:27:00+00:00",
            "Class": "Support",
            "Xp": 112000,
            "PreviousXp": 110000,
            "BaselineId": "c8574f78-0c53-4e81-a3e2-d8194eb7c212",
            "BaselineVersion": 2,
            "Source": "SnapshotHotkey",
            "Screen": "Unlock",
            "Message": "XP rebased mid match. May not be accurate",
            "Observation": {
              "Id": "01f83d34-8ef1-4394-849a-8e6107051afc",
              "Class": "Support",
              "Xp": 112000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:26:00+00:00",
              "Source": "SnapshotHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 112000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "f2ab1fe3-47e4-46bd-95cb-ef80cf79c088",
            "Kind": "CurrentXp",
            "At": "2026-10-17T15:29:00+00:00",
            "Class": "Support",
            "Xp": 113000,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "CheckHotkey",
            "Screen": "Unlock",
            "Message": "Accepted current XP.",
            "Observation": {
              "Id": "2c57a6c9-5b75-4b77-81e1-3efdf7594d9e",
              "Class": "Support",
              "Xp": 113000,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:28:00+00:00",
              "Source": "CheckHotkey",
              "Screen": "Unlock",
              "RawText": "MOCK 113000/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "63a2553c-a151-45e9-88a7-c87e838fd6a3",
            "Kind": "EndDetected",
            "At": "2026-10-17T15:32:00+00:00",
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
            "Id": "302455cb-bcbd-4629-97b3-bdba4cbe27e0",
            "Kind": "FinalCandidate",
            "At": "2026-10-17T15:36:00+00:00",
            "Class": "Support",
            "Xp": 116500,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": "AutomaticOcr",
            "Screen": "EndProgression",
            "Message": "Observed candidate final total. Bonuses can be included.",
            "Observation": {
              "Id": "3409db3d-e6d7-4aac-bf7b-fc26e7d1820d",
              "Class": "Support",
              "Xp": 116500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:30:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 116500/1000000XP",
              "Confidence": 1
            }
          },
          {
            "Id": "8588794d-33fe-43c2-8eca-ce139f7cf158",
            "Kind": "FinalConfirmed",
            "At": "2026-10-17T15:39:00+00:00",
            "Class": null,
            "Xp": null,
            "PreviousXp": null,
            "BaselineId": null,
            "BaselineVersion": null,
            "Source": null,
            "Screen": null,
            "Message": "Observed final totals confirmed. Live earned-XP checks were not advanced.",
            "Observation": null
          }
        ],
        "PendingFinal": null,
        "ConfirmedFinal": {
          "SessionId": "1968524d-cb13-4a41-a2cc-1a81bcc59afc",
          "FirstObservedAt": "2026-10-17T15:33:00+00:00",
          "LastObservedAt": "2026-10-17T15:35:00+00:00",
          "ConfirmedAt": "2026-10-17T15:38:00+00:00",
          "Tracks": {
            "Support": {
              "Id": "7cc70fb8-74a3-418a-866c-831e9a906310",
              "Class": "Support",
              "Xp": 116500,
              "NextThreshold": 1000000,
              "CurrentLevel": null,
              "ThresholdLevel": null,
              "At": "2026-10-17T15:30:00+00:00",
              "Source": "AutomaticOcr",
              "Screen": "EndProgression",
              "RawText": "MOCK 116500/1000000XP",
              "Confidence": 1
            }
          },
          "PossiblePlaces": [
            2
          ],
          "PlacementEvidence": [
            {
              "At": "2026-10-17T15:37:00+00:00",
              "Source": "Manual",
              "PossiblePlaces": [
                2
              ],
              "RawText": "User-confirmed placement"
            }
          ],
          "HasConflict": false,
          "ConflictingTracks": [],
          "KeepOldReferencesChosen": false
        },
        "ArmedRebase": null,
        "RebaseInvalidatesOriginal": false,
        "HasUnlockMarker": true,
        "PostEndUnlockObserved": false
      }
    ],
    "ActiveSessionId": "1968524d-cb13-4a41-a2cc-1a81bcc59afc",
    "EnabledTracks": {
      "Assault": false,
      "Medic": false,
      "Recon": false,
      "Support": true,
      "Driver": false,
      "Pilot": false
    }
  }
}
```
