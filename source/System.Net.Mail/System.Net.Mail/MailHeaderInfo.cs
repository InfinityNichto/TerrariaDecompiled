using System.Collections.Generic;

namespace System.Net.Mail;

internal static class MailHeaderInfo
{
	private readonly struct HeaderInfo
	{
		public readonly string NormalizedName;

		public readonly bool IsSingleton;

		public readonly MailHeaderID ID;

		public readonly bool IsUserSettable;

		public readonly bool AllowsUnicode;

		public HeaderInfo(MailHeaderID id, string name, bool isSingleton, bool isUserSettable, bool allowsUnicode)
		{
			ID = id;
			NormalizedName = name;
			IsSingleton = isSingleton;
			IsUserSettable = isUserSettable;
			AllowsUnicode = allowsUnicode;
		}
	}

	private static readonly HeaderInfo[] s_headerInfo = new HeaderInfo[33]
	{
		new HeaderInfo(MailHeaderID.Bcc, "Bcc", isSingleton: true, isUserSettable: false, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.Cc, "Cc", isSingleton: true, isUserSettable: false, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.Comments, "Comments", isSingleton: false, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.ContentDescription, "Content-Description", isSingleton: true, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.ContentDisposition, "Content-Disposition", isSingleton: true, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.ContentID, "Content-ID", isSingleton: true, isUserSettable: false, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.ContentLocation, "Content-Location", isSingleton: true, isUserSettable: false, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.ContentTransferEncoding, "Content-Transfer-Encoding", isSingleton: true, isUserSettable: false, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.ContentType, "Content-Type", isSingleton: true, isUserSettable: false, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.Date, "Date", isSingleton: true, isUserSettable: false, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.From, "From", isSingleton: true, isUserSettable: false, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.Importance, "Importance", isSingleton: true, isUserSettable: false, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.InReplyTo, "In-Reply-To", isSingleton: true, isUserSettable: true, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.Keywords, "Keywords", isSingleton: false, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.Max, "Max", isSingleton: false, isUserSettable: true, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.MessageID, "Message-ID", isSingleton: true, isUserSettable: true, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.MimeVersion, "MIME-Version", isSingleton: true, isUserSettable: false, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.Priority, "Priority", isSingleton: true, isUserSettable: false, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.References, "References", isSingleton: true, isUserSettable: true, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.ReplyTo, "Reply-To", isSingleton: true, isUserSettable: false, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.ResentBcc, "Resent-Bcc", isSingleton: false, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.ResentCc, "Resent-Cc", isSingleton: false, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.ResentDate, "Resent-Date", isSingleton: false, isUserSettable: true, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.ResentFrom, "Resent-From", isSingleton: false, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.ResentMessageID, "Resent-Message-ID", isSingleton: false, isUserSettable: true, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.ResentSender, "Resent-Sender", isSingleton: false, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.ResentTo, "Resent-To", isSingleton: false, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.Sender, "Sender", isSingleton: true, isUserSettable: false, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.Subject, "Subject", isSingleton: true, isUserSettable: false, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.To, "To", isSingleton: true, isUserSettable: false, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.XPriority, "X-Priority", isSingleton: true, isUserSettable: false, allowsUnicode: false),
		new HeaderInfo(MailHeaderID.XReceiver, "X-Receiver", isSingleton: false, isUserSettable: true, allowsUnicode: true),
		new HeaderInfo(MailHeaderID.XSender, "X-Sender", isSingleton: true, isUserSettable: true, allowsUnicode: true)
	};

	private static readonly Dictionary<string, int> s_headerDictionary = CreateHeaderDictionary();

	private static Dictionary<string, int> CreateHeaderDictionary()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(33, StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < s_headerInfo.Length; i++)
		{
			dictionary.Add(s_headerInfo[i].NormalizedName, i);
		}
		return dictionary;
	}

	internal static string GetString(MailHeaderID id)
	{
		if (id == MailHeaderID.Unknown || id == (MailHeaderID)33)
		{
			return null;
		}
		return s_headerInfo[(int)id].NormalizedName;
	}

	internal static bool IsUserSettable(string name)
	{
		if (s_headerDictionary.TryGetValue(name, out var value))
		{
			return s_headerInfo[value].IsUserSettable;
		}
		return true;
	}

	internal static bool IsSingleton(string name)
	{
		if (s_headerDictionary.TryGetValue(name, out var value))
		{
			return s_headerInfo[value].IsSingleton;
		}
		return false;
	}

	internal static string NormalizeCase(string name)
	{
		if (!s_headerDictionary.TryGetValue(name, out var value))
		{
			return name;
		}
		return s_headerInfo[value].NormalizedName;
	}

	internal static bool AllowsUnicode(string name)
	{
		if (s_headerDictionary.TryGetValue(name, out var value))
		{
			return s_headerInfo[value].AllowsUnicode;
		}
		return true;
	}
}
