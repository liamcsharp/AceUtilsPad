using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AUP;

public sealed class KillSwitch : MonoBehaviour
{
	private const int RequestTimeoutMilliseconds = 10000;

	private MenuSettings settings;
	private float nextCheckTime;
	private int checking;
	private volatile string pendingBody;
	private volatile string pendingError;
	private bool warnedUnreadable;

	public KillSwitchState State { get; private set; }

	public string Status { get; private set; } = "Kill switch: checking...";

	public event Action Killed;

	public void Initialize(MenuSettings menuSettings)
	{
		settings = menuSettings;
	}

	private void Update()
	{
		if (settings == null || State == KillSwitchState.Dead)
		{
			return;
		}

		ApplyPendingResult();
		if (Time.unscaledTime >= nextCheckTime && Interlocked.CompareExchange(ref checking, 1, 0) == 0)
		{
			nextCheckTime = Time.unscaledTime + Mathf.Max(10, settings.KillSwitchIntervalSeconds.Value);
			string url = ResolveUrl(settings.KillSwitchUrl.Value);
			Task.Run(() => Fetch(url));
		}
	}

	private static string ResolveUrl(string configured)
	{
		string trimmed = (configured ?? string.Empty).Trim().TrimEnd('/');
		bool isDashboard = trimmed.Length == 0 || string.Equals(trimmed, MenuSettings.DashboardUrl, StringComparison.OrdinalIgnoreCase);
		return isDashboard ? MenuSettings.DefaultKillSwitchUrl : trimmed;
	}

	private void ApplyPendingResult()
	{
		string error = pendingError;
		string body = pendingBody;
		if (error == null && body == null)
		{
			return;
		}

		pendingError = null;
		pendingBody = null;

		if (error != null)
		{
			Status = "Kill switch: unreachable, menu stays on.";
			return;
		}

		KillSwitchState parsed = KillSwitchParser.Parse(body);
		switch (parsed)
		{
			case KillSwitchState.Dead:
				State = KillSwitchState.Dead;
				Status = "Kill switch: shut down remotely.";
				Killed?.Invoke();
				break;
			case KillSwitchState.Alive:
				State = KillSwitchState.Alive;
				Status = "Kill switch: online.";
				break;
			default:
				Status = "Kill switch: unreadable reply, menu stays on.";
				WarnUnreadable();
				break;
		}
	}

	private void WarnUnreadable()
	{
		if (warnedUnreadable)
		{
			return;
		}

		warnedUnreadable = true;
		Debug.LogWarning("[AceUtilsPad] The kill switch URL returned a page instead of an online/offline answer. "
						 + "Set KillSwitch.Url in the config to a link that answers online or offline.");
	}

	private void Fetch(string url)
	{
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Timeout = RequestTimeoutMilliseconds;
			request.ReadWriteTimeout = RequestTimeoutMilliseconds;
			request.UserAgent = PluginInfo.Name + "/" + PluginInfo.Version;
			request.Accept = "application/json, text/plain";
			request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

			using (WebResponse response = request.GetResponse())
			using (StreamReader reader = new StreamReader(response.GetResponseStream()))
			{
				pendingBody = reader.ReadToEnd();
			}
		}
		catch (Exception exception)
		{
			pendingError = exception.Message;
		}
		finally
		{
			Interlocked.Exchange(ref checking, 0);
		}
	}
}
