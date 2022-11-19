using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace Nextended.Core.Types
{
	/// <summary>
	/// Währung
	/// </summary>
	[DebuggerDisplay("{Symbol} Name={Name} | IsoCode={IsoCode} | NativeName={NativeName}")]
	[DataContract]
	public class Currency : NotificationObject, IEquatable<Currency>
	{
		private string nativeName;
		private string symbol;
		private object externalId;

		private static readonly object lockObj = new object();
		private static List<Currency> cache;

		private static readonly Lazy<IEnumerable<CultureInfo>> allCultures = new Lazy<IEnumerable<CultureInfo>>(() => CultureInfo.GetCultures(CultureTypes.AllCultures)
										  .Where(info => !info.IsNeutralCulture && !info.Equals(CultureInfo.InvariantCulture)));

		private static readonly Lazy<IEnumerable<RegionInfo>> allRegions = new(() => allCultures.Value.Where(s => s.LCID != 4096).Select(source =>
        {
            try
            {
				
                return new RegionInfo(source.LCID);
            }
            catch (Exception )
            {
                return null;
            }
        }).Where(info => info != null));

        private string isoCode;
		private string name;

		/// <summary>
		/// Id
		/// </summary>
		[DataMember(IsRequired = true)]
		public int Id { get; private set; }

		/// <summary>
		/// Name der währung
		/// </summary>
		[Required]
		[DataMember(IsRequired = true)]
		public string Name
		{
			get => name;
            set => SetProperty(ref name, value);
        }

		/// <summary>
		/// ISOCurrencySymbol
		/// </summary>
		[Required]
		[DataMember(IsRequired = true)]
		public string IsoCode
		{
			get => isoCode;
            set => SetProperty(ref isoCode, value);
        }

		
		/// <summary>
		/// Initializes a new instance of the <see cref="Currency" /> class.
		/// </summary>
		public Currency(string name, string isoCode)
			: this(GetRegionsForCurrencyISOCode(isoCode).FirstOrDefault())
		{
			Name = name;
			IsoCode = isoCode;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Currency" /> class.
		/// </summary>
		public Currency(string isoCode)
			: this(GetRegionsForCurrencyISOCode(isoCode).FirstOrDefault())
		{
			IsoCode = isoCode;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Currency" /> class.
		/// </summary>
		public Currency(CultureInfo cultureInfo)
			: this(new RegionInfo(cultureInfo.LCID))
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="Currency" /> class.
		/// </summary>
		/// <param name="regionInfo">The region info.</param>
		public Currency(RegionInfo regionInfo)
		{
			SetInfos(regionInfo);
		}

		/// <summary>
		/// ExternalId
		/// </summary>
		public object ExternalId
		{
			get => externalId;
            set => SetProperty(ref externalId, value);
        }

		/// <summary>
		/// Gibt an ob die Währung gültig ist
		/// </summary>
		public bool IsValid => Regions.Any();


        /// <summary>
		/// Nativer Name der Währung
		/// </summary>
		[DataMember]
		//[Display(Order = 2, AutoGenerateField = true, ResourceType = typeof(Resources), Name = "CurrencyNativeName", Description = "CurrencyNativeNameDescription", Prompt = "CurrencyNativeNameExample")]
		public string NativeName
		{
			get => nativeName;
            set => SetProperty(ref nativeName, value);
        }

		/// <summary>
		/// Symbol der Währung (z.b €)
		/// </summary>
		[Required]
		[DataMember(IsRequired = true)]
		//[Display(Order = 4, AutoGenerateField = true, ResourceType = typeof(Resources), Name = "CurrencySymbol", Description = "CurrencySymbolDescription", Prompt = "CurrencySymbolExample")]
		public string Symbol
		{
			get => symbol;
            set => SetProperty(ref symbol, value);
        }
		
		/// <summary>
		/// enthält alle Regionen für diese Währung
		/// </summary>
		/// <returns></returns>
		public IEnumerable<RegionInfo> Regions => GetRegionsForCurrencyISOCode(IsoCode);

        /// <summary>
		/// Gibt alle Cultures für diese Währung zurück
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CultureInfo> Cultures => GetCulturesForCurrencyISOCode(IsoCode);

        /// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return $"{Name} {Symbol} ({IsoCode})";
		}

		private void SetInfos(RegionInfo info, bool overrideExisting = false)
		{
			if (info != null)
			{
				if (string.IsNullOrEmpty(Name) || overrideExisting)
					Name = info.CurrencyEnglishName;
				if (string.IsNullOrEmpty(NativeName) || overrideExisting)
					NativeName = info.CurrencyNativeName;
				if (string.IsNullOrEmpty(Symbol) || overrideExisting)
					Symbol = info.CurrencySymbol;
				if (string.IsNullOrEmpty(IsoCode) || overrideExisting)
					IsoCode = info.ISOCurrencySymbol;
				if (ExternalId == null || overrideExisting)
					ExternalId = info.GeoId;
			}
		}

		#region Static

		/// <summary>
		/// Gibt alle möglichen Währungen zurück
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<Currency> All
		{
			get
			{
				lock (lockObj)
				{
					if (cache == null)
					{
						lock (lockObj)
						{
							cache = new List<Currency>();
							foreach (var ri in CultureInfo.GetCultures(CultureTypes.AllCultures)
							                              .Where(info => info.LCID != 4096 && !info.IsNeutralCulture && !info.Equals(CultureInfo.InvariantCulture))
                                                          .Select(source =>
                                                          {
                                                              try
                                                              {
                                                                  return new RegionInfo(source.LCID);
                                                              }
                                                              catch (Exception)
                                                              {
                                                                  return null;
                                                              }
                                                          })
							                              .Where(ri => ri != null && cache.All(currency => currency.IsoCode != ri.ISOCurrencySymbol)))
							{
								cache.Add(new Currency(ri));
							}
						}
					}
				}
				return cache;
			}
		}

		/// <summary>
		/// Versucht für den übergebenen string eine Currency zu finden
		/// </summary>
		public static Currency Find(string s)
		{
            return
				All.FirstOrDefault(
					c =>
					c.Name.ToUpper() == s.ToUpper() || c.NativeName.ToUpper() == s.ToUpper() || c.IsoCode.ToUpper() == s.ToUpper() ||
					c.Symbol == s);
		}

		/// <summary>
		/// Euro
		/// </summary>
		public static Currency Euro => Find("€");

        /// <summary>
		/// US-Dollar
		/// </summary>
		public static Currency USD => Find("$");

        public static string GetCurrencyNameForISOCode(string currencyIso)
        {
            var currencyName = string.Empty;
            foreach (var info in CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(info => !info.IsNeutralCulture && info.LCID != CultureInfo.InvariantCulture.LCID))
            {
                var regionInfo = new RegionInfo(info.LCID);
                if (string.Equals(regionInfo.ISOCurrencySymbol, currencyIso, StringComparison.InvariantCultureIgnoreCase))
                {
                    currencyName = regionInfo.CurrencyEnglishName;
                    break;
                }
            }
            return currencyName;
        }

        public static IEnumerable<CultureInfo> GetCulturesForCurrencyISOCode(string isoCode)
		{
			return from culture in allCultures.Value.Where(s => s.LCID != 4096)
				   let ri = new RegionInfo(culture.LCID)
				   where ri.ISOCurrencySymbol == isoCode
				   select culture;
		}

		private static IEnumerable<RegionInfo> GetRegionsForCurrencyISOCode(string isoCode)
		{
			return allRegions.Value.Where(ri => ri.ISOCurrencySymbol == isoCode);
		}

		#endregion

		#region Equality

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(Currency left, Currency right)
		{
			return Equals(left, right);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(Currency left, Currency right)
		{
			return !Equals(left, right);
		}


		/// <summary>
		/// Equalses the specified other.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public bool Equals(Currency other)
		{
			return Equals(ExternalId, other.ExternalId) && string.Equals(Name, other.Name) &&
				   string.Equals(IsoCode, other.IsoCode) && string.Equals(NativeName, other.NativeName) &&
				   string.Equals(Symbol, other.Symbol);
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((Currency)obj);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (ExternalId != null ? ExternalId.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (IsoCode != null ? IsoCode.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (NativeName != null ? NativeName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Symbol != null ? Symbol.GetHashCode() : 0);
				return hashCode;
			}
		}

		#endregion

	}
}