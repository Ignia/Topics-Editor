﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using Ignia.Topics.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ignia.Topics.Serialization {

  /*============================================================================================================================
  | CLASS: ATTRIBUTE VALUE COLLECTION (JSON CONVERTER)
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   A converter that JSON.NET can use to determine how to efficiently serialize <see cref="AttributeValueCollection"/>
  ///   instances.
  /// </summary>
  /// <remarks>
  ///   Out of the box, the <see cref="AttributeValueCollection"/> contains <see cref="AttributeValue"/> instances with
  ///   properties such as <see cref="AttributeValue.LastModified"/> and <see cref="AttributeValue.IsDirty"/>, which aren't
  ///   needed for serialization. Instead, the <see cref="AttributeValueCollectionJsonConverter"/> provides a more efficient
  ///   format that exclusively includes the key/value pairs.
  /// </remarks>
  public class AttributeValueCollectionJsonConverter : JsonConverter {

    /*==========================================================================================================================
    | PROPERTY: CAN CONVERT
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Given a type, allows the <see cref="AttributeValueCollectionJsonConverter"/> to determine whether it's capable of
    ///   converting that type.
    /// </summary>
    /// <param name="objectType">An instance of the object being converted.</param>
    /// <returns></returns>
    public override bool CanConvert(Type objectType) => typeof(AttributeValueCollection).IsAssignableFrom(objectType);

    /*==========================================================================================================================
    | METHOD: WRITE JSON
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Reads the supplied <paramref name="value"/>, and converts it to a JSON object.
    /// </summary>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      writer.WriteStartObject();
      foreach (var attribute in (AttributeValueCollection)value) {
        writer.WritePropertyName(attribute.Key);
        serializer.Serialize(writer, attribute.Value);
      }
      writer.WriteEndObject();
    }

    /*==========================================================================================================================
    | PROPERTY: CAN READ
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Informs the serialization library whether or not this the <see cref="AttributeValueCollectionJsonConverter"/> is
    ///   capable of reading JSON data.
    /// </summary>
    public override bool CanRead => true;

    /*==========================================================================================================================
    | METHOD: READ JSON
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Reads the JSON input, and populates the supplied <paramref name="existingValue"/>.
    /// </summary>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate suitability
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (!CanConvert(objectType)) {
        throw new NotImplementedException(
          $"The {nameof(AttributeValueCollectionJsonConverter)} cannot read objects of type {objectType.Name}"
        );
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate request type
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (reader.TokenType == JsonToken.Null) {
        return null;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Ensure object is created
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (existingValue == null) {
        existingValue = new AttributeValueCollection(null);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Populate existing value
      \-----------------------------------------------------------------------------------------------------------------------*/
      var attributes            = (AttributeValueCollection)existingValue;
      var jObject               = JObject.Load(reader);

      foreach (var property in jObject.Properties()) {
        attributes.SetValue(property.Name, property.Value.ToString());
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Return results
      \-----------------------------------------------------------------------------------------------------------------------*/
      return attributes;

    }

  } //Class
} //Namespace