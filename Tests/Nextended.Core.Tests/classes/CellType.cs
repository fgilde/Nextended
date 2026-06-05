using System.Xml.Serialization;

namespace Nextended.Core.Tests.classes
{
    /// <summary>
    ///     Zellentyp
    /// </summary>
    public enum CellType
    {
        /// <summary>
        ///     Leere Zelle
        /// </summary>
        [XmlEnum("RCT_EMPTY")] 
        Empty,

        /// <summary>
        ///     Feldname
        /// </summary>
        [XmlEnum("RCT_FIELDNAME")]
        FieldName,

        /// <summary>
        ///     Beschreibung ("Langname")
        /// </summary>
        [XmlEnum("RCT_FIELDDESCRIPTION")]
        FieldDescription,

        /// <summary>
        ///     Konto
        /// </summary>
        [XmlEnum("RCT_FIELDACCOUNT")]
        FieldAccount,

        /// <summary>
        ///     Feld-Einheit
        /// </summary>
        [XmlEnum("RCT_FIELDUNIT")]
        FieldUnit,

        /// <summary>
        ///     Feldnotiz
        /// </summary>
        [XmlEnum("RCT_FIELDMEMO")]
        FieldMemo,

        /// <summary>
        ///     Zahlenwert
        /// </summary>
        [XmlEnum("RCT_DOUBLE")] 
        Double,

        /// <summary>
        ///     Zahlenwert mit Warnfarbe
        /// </summary>
        [XmlEnum("RCT_DOUBLE_COLOR")]
        DoubleWarnlight,

        /// <summary>
        ///     Zellkommentar
        /// </summary>
        [XmlEnum("RCT_CELLCOMMENT")]
        CellComment,

        /// <summary>
        ///     Ebenenbezeichnung
        /// </summary>
        [XmlEnum("RCT_LAYERNAME")]
        LayerName,

        /// <summary>
        ///     Organisationsbezeichnung
        /// </summary>
        [XmlEnum("RCT_ORGANISATIONNAME")] 
        OrganisationName,

        /// <summary>
        ///     Importierte Daten
        /// </summary>
        [XmlEnum("RCT_ACTUAL")]
        Actual,

        /// <summary>
        ///     Importierte Daten (OE)
        /// </summary>
        [XmlEnum("RCT_ACTUAL_ORG")]
        ActualOrganisation,

        /// <summary>
        ///     Text
        /// </summary>
        [XmlEnum("RCT_STRING")]
        Text,

        /// <summary>
        ///     Parameter eines Logikbausteins
        /// </summary>
        [XmlEnum("RCT_PARAM")]
        Parameter,

        /// <summary>
        ///     Name eines Logikbaustein-Parameters
        /// </summary>
        [XmlEnum("RCT_PARAMNAME")]
        ParameterName,

        /// <summary>
        ///     Name eines Logikbausteins
        /// </summary>
        [XmlEnum("RCT_BLBNAME")]
        BlbName,

        /// <summary>
        ///     Eingang eines Logikbausteins
        /// </summary>
        [XmlEnum("RCT_BLBINPNAME")]
        BlbInputName,

        /// <summary>
        ///     Ausgang eines Logikbausteins
        /// </summary>
        [XmlEnum("RCT_BLBOUTNAME")]
        BlbOutputName,

        /// <summary>
        ///     Kontonummer eines LB-Eingangs
        ///     oder Ausgangs
        /// </summary>
        [XmlEnum("RCT_ACCOUNTNUMBER")]
        AccountNumber,

        /// <summary>
        ///     Name eines Währungspärchens zur Kursumrechnung
        /// </summary>
        [XmlEnum("RCT_CURRENCYMAPPING")]
        CurrencyMapping,

        /// <summary>
        ///     Bezeichnung eines Kurstyps
        ///     (Stichtags-, Durchschnittskurs usw.)
        /// </summary>
        [XmlEnum("RCT_RATETYPE")]
        RateType,
    }
}