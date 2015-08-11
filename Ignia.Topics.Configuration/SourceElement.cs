/*==============================================================================================================================
| Author        Katherine Trunkey, Ignia LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Configuration;
using System.Globalization;
using System.Web;
using System.Web.Security;

namespace Ignia.Topics.Configuration {

  /*============================================================================================================================
  | CLASS: SOURCE ELEMENT
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Provides a custom <see cref="ConfigurationElement"/> implementation which represents a custom configuration source
  ///   (e.g., for versioning, the OnTopic editor, etc.). Allows the application to retrieve configuration data from multiple
  ///   sources.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Inherits from .
  ///   </para>
  ///   <para>
  ///     Adapted DIRECTLY from the Ignia Localization library; in the future, these libraries may (and should) share custom
  ///     configuration classes.
  ///   </para>
  /// </remarks>
  public class SourceElement : ConfigurationElement {

    /*==========================================================================================================================
    | ATTRIBUTE: SOURCE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets the source for the configuration setting.
    /// </summary>
    [ConfigurationProperty("source", DefaultValue="QueryString", IsRequired=true, IsKey=true)]
    public string Source {
      get {
        return this["source"] as string;
      }
    }

    /*==========================================================================================================================
    | ATTRIBUTE: ENABLED
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets a value indicating whether this <see cref="SourceElement"/> is enabled.
    /// </summary>
    [ConfigurationProperty("enabled", DefaultValue="True", IsRequired=false)]
    public bool Enabled {
      get {
        return Convert.ToBoolean(this["enabled"], CultureInfo.InvariantCulture);
      }
    }

    /*==========================================================================================================================
    | ATTRIBUTE: LOCATION
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets the location attribute value.
    /// </summary>
    [ConfigurationProperty("location", IsRequired=false)]
    public string Location {
      get {
        return this["location"] as string;
      }
    }

    /*==========================================================================================================================
    | ATTRIBUTE: TRUSTED
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets a value indicating whether this <see cref="SourceElement"/> is trusted.
    /// </summary>
    [ConfigurationProperty("trusted", DefaultValue="False", IsRequired=false)]
    public bool Trusted {
      get {
        return Convert.ToBoolean(this["trusted"], CultureInfo.InvariantCulture);
      }
    }

    /*==========================================================================================================================
    | METHOD: GET ELEMENT
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets the source element given the parent element or collection and expected name.
    /// </summary>
    /// <param name="parent">
    ///   The parent <see cref="ConfigurationElement"/> or <see cref="ConfigurationElementCollection"/>.
    /// </param>
    /// <param name="key">The string key (expected name).</param>
    public static SourceElement GetElement(ConfigurationElement parent, string key) {
      if (parent == null) return null;
      return (SourceElement)parent.ElementInformation.Properties[key].Value;
    }

    public static SourceElement GetElement(ConfigurationElementCollection parent, string key) {
      if (parent == null) return null;
      foreach (SourceElement source in parent) {
        if (source.Source.Equals(key)) {
          return source;
        }
      }
      return null;
    }

    /*==========================================================================================================================
    | METHOD: GET VALUE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Looks up a source element at a given location and, assuming it's enabled, identifies the source value.
    /// </summary>
    /// <param name="parent">
    ///   The parent <see cref="ConfigurationElement"/> or <see cref="ConfigurationElementCollection"/>.
    /// </param>
    /// <param name="key">The string key (expected name).</param>
    /// <returns>Returns the target value, if found, or null.</returns>
    public static string GetValue(ConfigurationElement parent, string key) {
      return GetValue(GetElement(parent, key));
    }

    public static string GetValue(ConfigurationElementCollection parent, string key) {
      return GetValue(GetElement(parent, key));
    }

    public static string GetValue(SourceElement element) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Return null if the element is disabled or missing
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (element == null || !element.Enabled) return null;

      /*------------------------------------------------------------------------------------------------------------------------
      | Pull value from support source
      \-----------------------------------------------------------------------------------------------------------------------*/
      string value = null;

      switch (element.Source.ToUpperInvariant()) {
        case("QUERYSTRING") :
          value         = HttpContext.Current.Request.QueryString[element.Location];
          break;
        case("FORM") :
          value         = HttpContext.Current.Request.Form[element.Location];
          break;
        case("APPLICATION") :
          value         = (string)HttpContext.Current.Application[element.Location];
          break;
        case("SESSION") :
          value         = (string)HttpContext.Current.Session[element.Location];
          break;
        case("COOKIE") :
          if (HttpContext.Current.Request.Cookies[element.Location] != null) {
            value       = HttpContext.Current.Request.Cookies[element.Location].Value;
            }
          break;
        case("ROLE") :
          value         = Roles.IsUserInRole(element.Location).ToString();
          break;
        case("HOSTNAME") :
          value         = element.Location;
          break;
        case("URL") :
          value         = HttpContext.Current.Request.Path.Split('/')[Int32.Parse(element.Location, CultureInfo.InvariantCulture)];
          break;
        default :
          throw new ConfigurationErrorsException("The source '" + element.Source + "' in the web.config is invalid.");
      }

      return value;

    }

    /*==========================================================================================================================
    | METHOD: IS ENABLED
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Looks up a source element at a given location, identifies the source value and returns a boolean value representing
    ///   whether or not the source is available, enabled or set to true.
    /// </summary>
    /// <param name="parent">
    ///   The parent <see cref="ConfigurationElement"/>, <see cref="ConfigurationElementCollection"/>, or
    ///   <see cref="SourceElement"/>.
    /// </param>
    /// <param name="key">The string key (expected name).</param>
    /// <param name="evaluateValue">Boolean indicator noting whether to use the Value to determine enabled status.</param>
    public static bool IsEnabled(ConfigurationElement parent, string key) {
      return IsEnabled(parent, key, true);
    }

    public static bool IsEnabled(ConfigurationElement parent, string key, bool evaluateValue) {
      return IsEnabled(GetElement(parent, key), evaluateValue);
    }

    public static bool IsEnabled(ConfigurationElementCollection parent, string key) {
      return IsEnabled(parent, key, false);
    }

    public static bool IsEnabled(ConfigurationElementCollection parent, string key, bool evaluateValue) {
      return IsEnabled(GetElement(parent, key), evaluateValue);
    }

    public static bool IsEnabled(SourceElement element, bool evaluateValue) {

      if (element == null) {
        return false;
      }
      if (!element.Enabled) {
        return false;
      }
      else if (!evaluateValue || element.Location == null) {
        return true;
      }

      string value = GetValue(element);

      if (String.IsNullOrEmpty(value)) return false;

      return Convert.ToBoolean(value, CultureInfo.InvariantCulture);

    }

    /*==========================================================================================================================
    | METHOD: IS TRUSTED
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Determines whether the specified parent is trusted.
    /// </summary>
    /// <param name="parent">
    ///   The parent <see cref="ConfigurationElement"/> or <see cref="ConfigurationElementCollection"/>.
    /// </param>
    /// <param name="key">The string key (expected name).</param>
    /// <param name="element">The <see cref="SourceElement"/> object.</param>
    public static bool IsTrusted(ConfigurationElement parent, string key) {
      return IsTrusted(GetElement(parent, key));
    }

    public static bool IsTrusted(ConfigurationElementCollection parent, string key) {
      return IsTrusted(GetElement(parent, key));
    }

    public static bool IsTrusted(SourceElement element) {
      return (element == null)? false : element.Trusted;
    }

  } //Class

} //Namespace
