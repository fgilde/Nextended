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

		[Obsolete("Only for serializer")]
        public Currency()
        {
			// For serialize only
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
		public static Currency USD => Find("USD");
        public static Currency AED => Find("AED");  // Vereinigte Arabische Emirate Dirham
        public static Currency AFN => Find("AFN");  // Afghani
        public static Currency ALL => Find("ALL");  // Albanischer Lek
        public static Currency AMD => Find("AMD");  // Armenischer Dram
        public static Currency ANG => Find("ANG");  // Niederländische-Antillen-Gulden
        public static Currency AOA => Find("AOA");  // Angolanischer Kwanza
        public static Currency ARS => Find("ARS");  // Argentinischer Peso
        public static Currency AUD => Find("AUD");  // Australischer Dollar
        public static Currency AWG => Find("AWG");  // Aruba-Florin
        public static Currency AZN => Find("AZN");  // Aserbaidschan-Manat
        public static Currency BAM => Find("BAM");  // Konvertible Mark (Bosnien-Herzegowina)
        public static Currency BBD => Find("BBD");  // Barbados-Dollar
        public static Currency BDT => Find("BDT");  // Bangladesch Taka
        public static Currency BGN => Find("BGN");  // Bulgarischer Lew
        public static Currency BHD => Find("BHD");  // Bahrain-Dinar
        public static Currency BIF => Find("BIF");  // Burundi-Franc
        public static Currency BMD => Find("BMD");  // Bermuda-Dollar
        public static Currency BND => Find("BND");  // Brunei-Dollar
        public static Currency BOB => Find("BOB");  // Boliviano
        public static Currency BRL => Find("BRL");  // Brasilianischer Real
        public static Currency BSD => Find("BSD");  // Bahama-Dollar
        public static Currency BTN => Find("BTN");  // Ngultrum (Bhutan)
        public static Currency BWP => Find("BWP");  // Pula (Botsuana)
        public static Currency BYN => Find("BYN");  // Weißrussischer Rubel
        public static Currency BZD => Find("BZD");  // Belize-Dollar
        public static Currency CAD => Find("CAD");  // Kanadischer Dollar
        public static Currency CDF => Find("CDF");  // Kongolesischer Franc
        public static Currency CHF => Find("CHF");  // Schweizer Franken
        public static Currency CLP => Find("CLP");  // Chilenischer Peso
        public static Currency CNY => Find("CNY");  // Renminbi Yuan (China)
        public static Currency COP => Find("COP");  // Kolumbianischer Peso
        public static Currency CRC => Find("CRC");  // Costa-Rica-Colón
        public static Currency CUP => Find("CUP");  // Kubanischer Peso
        public static Currency CVE => Find("CVE");  // Kap-Verde-Escudo
        public static Currency CZK => Find("CZK");  // Tschechische Krone
        public static Currency DJF => Find("DJF");  // Dschibuti-Franc
        public static Currency DKK => Find("DKK");  // Dänische Krone
        public static Currency DOP => Find("DOP");  // Dominikanischer Peso
        public static Currency DZD => Find("DZD");  // Algerischer Dinar
        public static Currency EGP => Find("EGP");  // Ägyptisches Pfund
        public static Currency ERN => Find("ERN");  // Eritreischer Nakfa
                                                    // EUR wird explizit *nicht* aufgeführt, wie gewünscht
        public static Currency ETB => Find("ETB");  // Äthiopischer Birr
        public static Currency FJD => Find("FJD");  // Fidschi-Dollar
        public static Currency FKP => Find("FKP");  // Falkland-Pfund
        public static Currency GBP => Find("GBP");  // Pfund Sterling
        public static Currency GEL => Find("GEL");  // Georgischer Lari
        public static Currency GGP => Find("GGP");  // Guernsey-Pfund (inoffiziell, teils ISO-Variante)
        public static Currency GHS => Find("GHS");  // Ghanaischer Cedi
        public static Currency GIP => Find("GIP");  // Gibraltar-Pfund
        public static Currency GMD => Find("GMD");  // Gambischer Dalasi
        public static Currency GNF => Find("GNF");  // Guinea-Franc
        public static Currency GTQ => Find("GTQ");  // Quetzal (Guatemala)
        public static Currency GYD => Find("GYD");  // Guyana-Dollar
        public static Currency HKD => Find("HKD");  // Hongkong-Dollar
        public static Currency HNL => Find("HNL");  // Lempira (Honduras)
        public static Currency HRK => Find("HRK");  // Kroatische Kuna (seit 2023 durch EUR ersetzt, formal aber ISO-Code)
        public static Currency HTG => Find("HTG");  // Gourde (Haiti)
        public static Currency HUF => Find("HUF");  // Ungarischer Forint
        public static Currency IDR => Find("IDR");  // Indonesische Rupiah
        public static Currency ILS => Find("ILS");  // Israelischer Schekel
        public static Currency IMP => Find("IMP");  // Isle-of-Man-Pfund (ggf. kein offizieller ISO-4217-Code)
        public static Currency INR => Find("INR");  // Indische Rupie
        public static Currency IQD => Find("IQD");  // Irakischer Dinar
        public static Currency IRR => Find("IRR");  // Iranischer Rial
        public static Currency ISK => Find("ISK");  // Isländische Krone
        public static Currency JEP => Find("JEP");  // Jersey-Pfund (ggf. kein offizieller ISO-4217-Code)
        public static Currency JMD => Find("JMD");  // Jamaika-Dollar
        public static Currency JOD => Find("JOD");  // Jordanischer Dinar
        public static Currency JPY => Find("JPY");  // Japanischer Yen
        public static Currency KES => Find("KES");  // Kenianischer Schilling
        public static Currency KGS => Find("KGS");  // Kirgisischer Som
        public static Currency KHR => Find("KHR");  // Kambodschanischer Riel
        public static Currency KMF => Find("KMF");  // Komoren-Franc
        public static Currency KPW => Find("KPW");  // Nordkoreanischer Won
        public static Currency KRW => Find("KRW");  // Südkoreanischer Won
        public static Currency KWD => Find("KWD");  // Kuwait-Dinar
        public static Currency KYD => Find("KYD");  // Kaiman-Dollar
        public static Currency KZT => Find("KZT");  // Tenge (Kasachstan)
        public static Currency LAK => Find("LAK");  // Laotischer Kip
        public static Currency LBP => Find("LBP");  // Libanesisches Pfund
        public static Currency LKR => Find("LKR");  // Sri-Lanka-Rupie
        public static Currency LRD => Find("LRD");  // Liberianischer Dollar
        public static Currency LSL => Find("LSL");  // Loti (Lesotho)
        public static Currency LYD => Find("LYD");  // Libyscher Dinar
        public static Currency MAD => Find("MAD");  // Marokkanischer Dirham
        public static Currency MDL => Find("MDL");  // Moldauischer Leu
        public static Currency MGA => Find("MGA");  // Madagaskar-Ariary
        public static Currency MKD => Find("MKD");  // Mazedonischer Denar
        public static Currency MMK => Find("MMK");  // Birmanischer Kyat
        public static Currency MNT => Find("MNT");  // Mongolischer Tögrög
        public static Currency MOP => Find("MOP");  // Pataca (Macao)
        public static Currency MRU => Find("MRU");  // Ouguiya (Mauretanien)
        public static Currency MUR => Find("MUR");  // Mauritius-Rupie
        public static Currency MVR => Find("MVR");  // Rufiyaa (Malediven)
        public static Currency MWK => Find("MWK");  // Malawi-Kwacha
        public static Currency MXN => Find("MXN");  // Mexikanischer Peso
        public static Currency MYR => Find("MYR");  // Malaysischer Ringgit
        public static Currency MZN => Find("MZN");  // Metical (Mosambik)
        public static Currency NAD => Find("NAD");  // Namibia-Dollar
        public static Currency NGN => Find("NGN");  // Naira (Nigeria)
        public static Currency NIO => Find("NIO");  // Córdoba Oro (Nicaragua)
        public static Currency NOK => Find("NOK");  // Norwegische Krone
        public static Currency NPR => Find("NPR");  // Nepalesische Rupie
        public static Currency NZD => Find("NZD");  // Neuseeland-Dollar
        public static Currency OMR => Find("OMR");  // Omanischer Rial
        public static Currency PAB => Find("PAB");  // Balboa (Panama)
        public static Currency PEN => Find("PEN");  // Peruanischer Sol
        public static Currency PGK => Find("PGK");  // Kina (Papua-Neuguinea)
        public static Currency PHP => Find("PHP");  // Philippinischer Peso
        public static Currency PKR => Find("PKR");  // Pakistanische Rupie
        public static Currency PLN => Find("PLN");  // Polnischer Złoty
        public static Currency PYG => Find("PYG");  // Guaraní (Paraguay)
        public static Currency QAR => Find("QAR");  // Katar-Riyal
        public static Currency RON => Find("RON");  // Rumänischer Leu
        public static Currency RSD => Find("RSD");  // Serbischer Dinar
        public static Currency RUB => Find("RUB");  // Russischer Rubel
        public static Currency RWF => Find("RWF");  // Ruanda-Franc
        public static Currency SAR => Find("SAR");  // Saudi-Riyal
        public static Currency SBD => Find("SBD");  // Salomonen-Dollar
        public static Currency SCR => Find("SCR");  // Seychellen-Rupie
        public static Currency SDG => Find("SDG");  // Sudan-Pfund
        public static Currency SEK => Find("SEK");  // Schwedische Krone
        public static Currency SGD => Find("SGD");  // Singapur-Dollar
        public static Currency SHP => Find("SHP");  // St. Helena-Pfund
        public static Currency SLL => Find("SLL");  // Leone (Sierra Leone)
        public static Currency SOS => Find("SOS");  // Somali-Schilling
        public static Currency SRD => Find("SRD");  // Suriname-Dollar
        public static Currency SSP => Find("SSP");  // Südsudanesisches Pfund
        public static Currency STN => Find("STN");  // Dobra (São Tomé und Príncipe)
        public static Currency SVC => Find("SVC");  // El-Salvador-Colón
        public static Currency SYP => Find("SYP");  // Syrisches Pfund
        public static Currency SZL => Find("SZL");  // Lilangeni (Eswatini)
        public static Currency THB => Find("THB");  // Thailändischer Baht
        public static Currency TJS => Find("TJS");  // Somoni (Tadschikistan)
        public static Currency TMT => Find("TMT");  // Turkmenistan-Manat
        public static Currency TND => Find("TND");  // Tunesischer Dinar
        public static Currency TOP => Find("TOP");  // Paʻanga (Tonga)
        public static Currency TRY => Find("TRY");  // Türkische Lira
        public static Currency TTD => Find("TTD");  // Trinidad-und-Tobago-Dollar
        public static Currency TVD => Find("TVD");  // Tuvalu-Dollar (inoffiziell, manchmal als AUD genutzt)
        public static Currency TWD => Find("TWD");  // Neuer Taiwan-Dollar
        public static Currency TZS => Find("TZS");  // Tansania-Schilling
        public static Currency UAH => Find("UAH");  // Ukrainische Hrywnja
        public static Currency UGX => Find("UGX");  // Uganda-Schilling        
        public static Currency UYU => Find("UYU");  // Uruguayischer Peso
        public static Currency UZS => Find("UZS");  // Usbekistan-Som
        public static Currency VED => Find("VED");  // Bolívar Digital (Venezuela, Nachfolger für VES)
        public static Currency VES => Find("VES");  // Bolívar Soberano (Venezuela, teils ersetzt von VED)
        public static Currency VND => Find("VND");  // Vietnamesischer Đồng
        public static Currency VUV => Find("VUV");  // Vatu (Vanuatu)
        public static Currency WST => Find("WST");  // Tala (Samoa)
        public static Currency XAF => Find("XAF");  // CFA-Franc BEAC
        public static Currency XCD => Find("XCD");  // Ostkaribischer Dollar
        public static Currency XOF => Find("XOF");  // CFA-Franc BCEAO
        public static Currency XPF => Find("XPF");  // CFP-Franc
        public static Currency YER => Find("YER");  // Jemen-Rial
        public static Currency ZAR => Find("ZAR");  // Rand (Südafrika)
        public static Currency ZMW => Find("ZMW");  // Sambischer Kwacha
        public static Currency ZWL => Find("ZWL");  // Simbabwe-Dollar

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