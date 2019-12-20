﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using Ignia.Topics.Attributes;
using Ignia.Topics.Collections;
using Ignia.Topics.Internal.Diagnostics;

namespace Ignia.Topics.Querying {

  /*============================================================================================================================
  | CLASS: TOPIC (EXTENSIONS)
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Provides extensions for querying <see cref="Ignia.Topics.Topic"/>.
  /// </summary>
  public static class TopicExtensions {

    /*==========================================================================================================================
    | METHOD: FIND FIRST
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Finds the first instance of a <see cref="Topic"/> in the topic tree that satisfies the delegate.
    /// </summary>
    /// <param name="topic">The instance of the <see cref="Topic"/> to operate against; populated automatically by .NET.</param>
    /// <param name="predicate">The function to validate whether a <see cref="Topic"/> should be included in the output.</param>
    /// <returns>The first instance of the topic to be satisfied.</returns>
    public static Topic? FindFirst(this Topic topic, Func<Topic, bool> predicate) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate contracts
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(topic, nameof(topic));
      Contract.Requires(predicate, nameof(predicate));

      /*------------------------------------------------------------------------------------------------------------------------
      | Search attributes
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (predicate(topic)) {
        return topic;
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Recurse over children
      \-----------------------------------------------------------------------------------------------------------------------*/
      foreach (var child in topic.Children) {
        var nestedResult = child.FindFirst(predicate);
        if (nestedResult != null) {
          return nestedResult;
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Indicate no results found
      \-----------------------------------------------------------------------------------------------------------------------*/
      return null;

    }

    /*==========================================================================================================================
    | METHOD: FIND ALL
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Retrieves a collection of topics based on a supplied function.
    /// </summary>
    /// <param name="topic">The instance of the <see cref="Topic"/> to operate against; populated automatically by .NET.</param>
    /// <param name="predicate">The function to validate whether a <see cref="Topic"/> should be included in the output.</param>
    /// <returns>A collection of topics matching the input parameters.</returns>
    public static ReadOnlyTopicCollection<Topic> FindAll(this Topic topic, Func<Topic, bool> predicate) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate contracts
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(topic, nameof(topic));
      Contract.Requires(predicate, nameof(predicate));

      /*------------------------------------------------------------------------------------------------------------------------
      | Search attributes
      \-----------------------------------------------------------------------------------------------------------------------*/
      var results = new TopicCollection();

      if (predicate(topic)) {
        results.Add(topic);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Recurse over children
      \-----------------------------------------------------------------------------------------------------------------------*/
      foreach (var child in topic.Children) {
        var nestedResults = child.FindAll(predicate);
        foreach (var matchedTopic in nestedResults) {
          if (!results.Contains(matchedTopic.Key)) {
            results.Add(matchedTopic);
          }
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Return results
      \-----------------------------------------------------------------------------------------------------------------------*/
      return results.AsReadOnly();

    }

    /*==========================================================================================================================
    | METHOD: FIND ALL BY ATTRIBUTE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Retrieves a collection of topics based on an attribute name and value.
    /// </summary>
    /// <param name="topic">The instance of the <see cref="Topic"/> to operate against; populated automatically by .NET.</param>
    /// <param name="name">The string identifier for the <see cref="AttributeValue"/> against which to be searched.</param>
    /// <param name="value">The text value for the <see cref="AttributeValue"/> against which to be searched.</param>
    /// <returns>A collection of topics matching the input parameters.</returns>
    /// <requires description="The attribute name must be specified." exception="T:System.ArgumentNullException">
    ///   !String.IsNullOrWhiteSpace(name)
    /// </requires>
    /// <requires
    ///   decription="The name should be an alphanumeric sequence; it should not contain spaces or symbols."
    ///   exception="T:System.ArgumentException">
    ///   !name.Contains(" ")
    /// </requires>
    public static ReadOnlyTopicCollection<Topic> FindAllByAttribute(this Topic topic, string name, string value) {

      /*------------------------------------------------------------------------------------------------------------------------
      | Validate contracts
      \-----------------------------------------------------------------------------------------------------------------------*/
      Contract.Requires(topic, "The topic parameter must be specified.");
      Contract.Requires<ArgumentNullException>(!String.IsNullOrWhiteSpace(name), "The attribute name must be specified.");
      Contract.Requires<ArgumentNullException>(!String.IsNullOrWhiteSpace(value), "The attribute value must be specified.");
      TopicFactory.ValidateKey(name);

      /*------------------------------------------------------------------------------------------------------------------------
      | Return results
      \-----------------------------------------------------------------------------------------------------------------------*/
      return topic.FindAll(t =>
        !String.IsNullOrEmpty(t.Attributes.GetValue(name)) &&
        t.Attributes.GetValue(name).IndexOf(value, StringComparison.InvariantCultureIgnoreCase) >= 0
      );

    }

  } //Class
} //Namespace