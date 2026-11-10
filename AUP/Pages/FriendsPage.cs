using System;
using AUP.UI;
using TMPro;

namespace AUP.Pages;

public sealed class FriendsPage : MenuPage
{
	private const int RowCount = 5;

	private int offset;

	public FriendsPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Friends";

	public override string IconId => "bell";

	private FriendAlertService Friends => Context.Friends;

	public override void Build(PageBuilder ui)
	{
		BuildOnlineSection(ui);
		BuildAlertSection(ui);
	}

	private void BuildOnlineSection(PageBuilder ui)
	{
		ui.Section("Online friends");
		ui.Label(() => Friends.Status, 1, 9.5f, FontStyles.Bold, Pick.Accent);
		for (int i = 0; i < RowCount; i++)
		{
			int row = i;
			ui.Row(() => RowText(row), "Join", () => JoinRow(row), () => RowAt(row) != null);
		}

		ui.Label(() => Friends.OnlineFriends.Count == 0 ? "None of your in-game friends are in a lobby." : string.Empty, 1, 8.5f, FontStyles.Normal, Pick.Muted);
		ui.Buttons(
			new ButtonSpec("< Prev", () => Scroll(-1), null, () => Friends.OnlineFriends.Count > RowCount),
			new ButtonSpec("Refresh", Friends.RefreshNow),
			new ButtonSpec("Next >", () => Scroll(1), null, () => Friends.OnlineFriends.Count > RowCount));
	}

	private void BuildAlertSection(PageBuilder ui)
	{
		ui.Section("Alerts");
		ui.Toggle("Friend Alerts", () => Context.Settings.FriendAlerts.Value, ToggleAlerts);
		ui.Label(LatestAlertText, 3, 9.5f);
		ui.Button("Join Latest Alert", JoinLatest, null, () => Friends.LatestAlert != null);
		ui.Label("Only friends you added at the in-game friend station show up here.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private FriendEntry RowAt(int row)
	{
		int count = Friends.OnlineFriends.Count;
		offset = Math.Max(0, Math.Min(offset, count - RowCount));
		int index = offset + row;
		return index < count ? Friends.OnlineFriends[index] : null;
	}

	private string RowText(int row)
	{
		FriendEntry friend = RowAt(row);
		if (friend == null)
		{
			return string.Empty;
		}

		string zone = string.IsNullOrEmpty(friend.Zone) ? string.Empty : "  <size=80%>" + Rich.Escape(friend.Zone) + "</size>";
		string privacy = friend.IsPublic ? string.Empty : " <size=80%>(private)</size>";
		return "<b>" + Rich.Escape(friend.Name) + "</b>" + zone + privacy;
	}

	private void JoinRow(int row)
	{
		FriendEntry friend = RowAt(row);
		if (friend != null)
		{
			Context.Lobby.JoinFriend(friend.Room, friend.IsPublic);
		}
	}

	private void Scroll(int direction)
	{
		int maxOffset = Math.Max(0, Friends.OnlineFriends.Count - RowCount);
		offset = Math.Max(0, Math.Min(maxOffset, offset + direction * RowCount));
	}

	private string LatestAlertText()
	{
		FriendEntry alert = Friends.LatestAlert;
		if (alert == null)
		{
			return "No friend alerts yet.";
		}

		string zone = string.IsNullOrEmpty(alert.Zone) ? string.Empty : " in " + Rich.Escape(alert.Zone);
		return "<b>" + Rich.Escape(alert.Name) + "</b> joined a lobby" + zone + ".";
	}

	private void JoinLatest()
	{
		FriendEntry alert = Friends.LatestAlert;
		if (alert != null)
		{
			Context.Lobby.JoinFriend(alert.Room, alert.IsPublic);
		}
	}

	private void ToggleAlerts()
	{
		Context.Settings.FriendAlerts.Value = !Context.Settings.FriendAlerts.Value;
	}
}
