using System.Diagnostics.CodeAnalysis;

namespace System.Globalization;

public class RegionInfo
{
	private string _name;

	private readonly CultureData _cultureData;

	internal static volatile RegionInfo s_currentRegionInfo;

	public static RegionInfo CurrentRegion
	{
		get
		{
			RegionInfo regionInfo = s_currentRegionInfo;
			if (regionInfo == null)
			{
				regionInfo = new RegionInfo(CultureData.GetCurrentRegionData());
				regionInfo._name = regionInfo._cultureData.RegionName;
				s_currentRegionInfo = regionInfo;
			}
			return regionInfo;
		}
	}

	public virtual string Name => _name;

	public virtual string EnglishName => _cultureData.EnglishCountryName;

	public virtual string DisplayName => _cultureData.LocalizedCountryName;

	public virtual string NativeName => _cultureData.NativeCountryName;

	public virtual string TwoLetterISORegionName => _cultureData.TwoLetterISOCountryName;

	public virtual string ThreeLetterISORegionName => _cultureData.ThreeLetterISOCountryName;

	public virtual string ThreeLetterWindowsRegionName => ThreeLetterISORegionName;

	public virtual bool IsMetric => _cultureData.MeasurementSystem == 0;

	public virtual int GeoId => _cultureData.GeoId;

	public virtual string CurrencyEnglishName => _cultureData.CurrencyEnglishName;

	public virtual string CurrencyNativeName => _cultureData.CurrencyNativeName;

	public virtual string CurrencySymbol => _cultureData.CurrencySymbol;

	public virtual string ISOCurrencySymbol => _cultureData.ISOCurrencySymbol;

	public RegionInfo(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_NoRegionInvariantCulture, "name");
		}
		_cultureData = CultureData.GetCultureDataForRegion(name, useUserOverride: true) ?? throw new ArgumentException(SR.Format(SR.Argument_InvalidCultureName, name), "name");
		if (_cultureData.IsNeutralCulture)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidNeutralRegionName, name), "name");
		}
		_name = _cultureData.RegionName;
	}

	public RegionInfo(int culture)
	{
		switch (culture)
		{
		case 127:
			throw new ArgumentException(SR.Argument_NoRegionInvariantCulture);
		case 0:
			throw new ArgumentException(SR.Format(SR.Argument_CultureIsNeutral, culture), "culture");
		case 3072:
			throw new ArgumentException(SR.Format(SR.Argument_CustomCultureCannotBePassedByNumber, culture), "culture");
		}
		_cultureData = CultureData.GetCultureData(culture, bUseUserOverride: true);
		_name = _cultureData.RegionName;
		if (_cultureData.IsNeutralCulture)
		{
			throw new ArgumentException(SR.Format(SR.Argument_CultureIsNeutral, culture), "culture");
		}
	}

	internal RegionInfo(CultureData cultureData)
	{
		_cultureData = cultureData;
		_name = _cultureData.RegionName;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is RegionInfo regionInfo)
		{
			return Name.Equals(regionInfo.Name);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Name.GetHashCode();
	}

	public override string ToString()
	{
		return Name;
	}
}
