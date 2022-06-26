/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/

namespace OnTopic.TestDoubles.Metadata {

  /*============================================================================================================================
  | CLASS: BOOLEAN (ATTRIBUTE DESCRIPTOR)
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Represents metadata for describing an boolean attribute type, including information on how it will be presented and
  ///   validated in the editor.
  /// </summary>
  /// <remarks>
  ///   This class is primarily used by the Topic Editor interface to determine how attributes are displayed as part of the
  ///   CMS; except in very specific scenarios, it is not typically used elsewhere in the Topic Library itself.
  /// </remarks>
  [ExcludeFromCodeCoverage]
  public class BooleanAttributeDescriptor : AttributeDescriptor {

    /*==========================================================================================================================
    | CONSTRUCTOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public BooleanAttributeDescriptor(
      string key,
      string contentType,
      Topic parent,
      int id = -1
    ) : base(
      key,
      contentType,
      parent,
      id
    ) {
    }

  } //Class
} //Namespace