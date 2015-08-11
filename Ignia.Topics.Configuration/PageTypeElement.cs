/*==============================================================================================================================
| Author        Katherine Trunkey, Ignia LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.ComponentModel;
using System.Configuration;

namespace Ignia.Topics.Configuration {

  /*============================================================================================================================
  | CLASS: PAGE TYPE ELEMENT
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Provides a custom <see cref="ConfigurationElement"/> which represents a page type
  ///   (default: <see cref="Ignia.Topics.Web.TopicPage"/>) as developed for the application.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Permits the application to define multiple page types.
  ///   </para>
  /// </remarks>
  public class PageTypeElement : ConfigurationElement {

    /*==========================================================================================================================
    | ATTRIBUTE: NAME
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets the name of the page type.
    /// </summary>
    [ConfigurationProperty("name", IsRequired=true, IsKey=true)]
    public string Name {
      get {
        return this["name"] as string;
      }
    }

    /*==========================================================================================================================
    | ATTRIBUTE: TYPE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Gets the page type class definition, including namespace if provided.
    /// </summary>
    [TypeConverter(typeof(TypeNameConverter))]
    [ConfigurationProperty("type", IsRequired = false)]
    public Type Type {
      get {
        return this["type"] as Type;
      }
    }

  } //Class

} //Namepsace
