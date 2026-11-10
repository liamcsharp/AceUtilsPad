using System.Collections.Generic;
using System.Text;
using AUP.UI;
using Photon.Pun;
using UnityEngine;

namespace AUP;

public sealed class DetectionAlertService : MonoBehaviour
{
	private const float ScanInterval = 2f;
	private const float AlertSeconds = 6f;

	private readonly Dictionary<string, string> notified = new Dictionary<string, string>();

	private MenuSettings settings;
	private AlertHub alerts;
	private float nextScanTime;

	public void Initialize(MenuSettings menuSettings, AlertHub alertHub)
	{
		settings = menuSettings;
		alerts = alertHub;
	}

	public void Forget()
	{
		notified.Clear();
	}

	private void Update()
	{
		if (settings == null || !settings.DetectionAlerts.Value || Time.unscaledTime < nextScanTime)
		{
			return;
		}

		nextScanTime = Time.unscaledTime + ScanInterval;
		if (!PhotonNetwork.InRoom)
		{
			notified.Clear();
			return;
		}

		foreach (VRRig rig in PlayerInfo.RemoteRigs())
		{
			List<DetectionResult> detections = DetectionService.Detect(rig);
			if (detections.Count == 0)
			{
				continue;
			}

			string key = PlayerInfo.StableKey(rig);
			string signature = DetectionService.Signature(detections);
			if (!notified.TryGetValue(key, out string previous) || previous != signature)
			{
				notified[key] = signature;
				alerts.Post(BuildAlert(rig, detections));
			}
		}
	}

	private static AlertMessage BuildAlert(VRRig rig, List<DetectionResult> detections)
	{
		StringBuilder builder = new StringBuilder();
		builder.Append("<b>").Append(Rich.Escape(PlayerInfo.DisplayName(rig))).Append(" detected:</b> ");
		for (int i = 0; i < detections.Count; i++)
		{
			DetectionResult detection = detections[i];
			if (i > 0)
			{
				builder.Append(", ");
			}

			builder.Append("<color=").Append(DetectionService.ColorFor(detection.Kind)).Append('>')
				.Append(detection.Kind).Append(": ").Append(Rich.Escape(detection.Name)).Append("</color>");
		}

		BanRisk worst = BanRiskInfo.Worst(detections);
		builder.Append("\n<size=85%><color=").Append(BanRiskInfo.Color(worst)).Append('>').Append(BanRiskInfo.Label(worst)).Append("</color></size>");
		return new AlertMessage { Text = builder.ToString(), Seconds = AlertSeconds };
	}
}
