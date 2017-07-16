/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Diagnostics.Contracts;

namespace Ignia.Topics.Web.Editor {

  /*============================================================================================================================
  | CLASS: FILE PATH
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Provides a strongly-typed class associated with the FilePath.ascx Attribute Type control and logic associated with
  ///   building a configured file path from values passed to the constructor.
  /// </summary>
  public class FilePath {

    /*==========================================================================================================================
    | CONSTRUCTOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Initializes a new instance of the <see cref="FilePath"/> class.
    /// </summary>
    public FilePath() { }

    /*==========================================================================================================================
    | METHOD: GET PATH
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Static helper method that returns a constructed file path based on evaluation and processing of the parameter
    ///   values/settings passed to the method.
    /// </summary>
    /// <param name="topic">The topic object.</param>
    /// <param name="attributeKey">The attribute key.</param>
    /// <param name="includeLeafTopic">Boolean indicator as to whether to include the endpoint/leaf topic in the path.</param>
    /// <param name="truncatePathAtTopic">The assembled topic keys at which to end the path string.</param>
    /// <returns>A constructed file path.</returns>
    public static string GetPath(
      Topic     topic,
      string    attributeKey,
      bool      includeLeafTopic        = true,
      string[]  truncatePathAtTopic     = null
      ) {

      /*----------------------------------------------------------------------------------------------------------------------
      | Validate return value
      \---------------------------------------------------------------------------------------------------------------------*/
      Contract.Ensures(Contract.Result<String>() != null);

      /*------------------------------------------------------------------------------------------------------------------------
      | Only process the path if both topic and attribtueKey are provided
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (topic == null || String.IsNullOrEmpty(attributeKey)) return "";

      /*------------------------------------------------------------------------------------------------------------------------
      | Build configured file path string base on values and settings paramters passed to the method
      \-----------------------------------------------------------------------------------------------------------------------*/
      string    filePath                = "";
      string    relativePath            = null;
      Topic     startTopic              = topic;
      Topic     endTopic                = includeLeafTopic? topic : topic.Parent;

      /*------------------------------------------------------------------------------------------------------------------------
      | Crawl up the topics tree to find file path values set at a higher level
      \-----------------------------------------------------------------------------------------------------------------------*/
      while (String.IsNullOrEmpty(filePath) && startTopic != null) {
        startTopic                      = startTopic.Parent;
        if (startTopic != null && !String.IsNullOrEmpty(attributeKey)) {
          filePath                      = startTopic.Attributes.Get(attributeKey);
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Add topic keys (directory names) between the start topic and the end topic based on the topic's webpath property
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (startTopic != null) {
        Contract.Assume(
          startTopic.WebPath.Length <= endTopic.WebPath.Length,
          "Assumes the startTopic path length is shorter than the endTopic path length."
          );
        relativePath                    = endTopic.WebPath.Substring(startTopic.WebPath.Length);
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Perform path truncation based on topics included in TruncatePathAtTopic
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (truncatePathAtTopic != null) {
        foreach (string truncationTopic in truncatePathAtTopic) {
          int truncateTopicLocation     = relativePath.IndexOf(truncationTopic, StringComparison.InvariantCultureIgnoreCase);
          if (truncateTopicLocation >= 0) {
            relativePath                = relativePath.Substring(0, truncateTopicLocation + truncationTopic.Length + 1);
          }
        }
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Add resulting relative path to the original file path (based on starting topic)
      \-----------------------------------------------------------------------------------------------------------------------*/
      filePath                         += relativePath;

      /*------------------------------------------------------------------------------------------------------------------------
      | Replace path slashes with backslahes if the resulting file path value uses a UNC or basic file path format
      \-----------------------------------------------------------------------------------------------------------------------*/
      if (filePath.IndexOf("\\") >= 0) {
        filePath                        = filePath.Replace("/", "\\");
      }

      /*------------------------------------------------------------------------------------------------------------------------
      | Return resulting file path
      \-----------------------------------------------------------------------------------------------------------------------*/
      return filePath;

    }

  } // Class

} // Namespace