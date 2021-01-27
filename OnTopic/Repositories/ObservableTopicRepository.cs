/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using OnTopic.Metadata;

namespace OnTopic.Repositories {

  /*============================================================================================================================
  | CLASS: OBSERVABLE TOPIC REPOSITORY
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Provides an abstract base class for <see cref="ITopicRepository"/> implementations which implements the event handling
  ///   logic.
  /// </summary>
  /// <remarks>
  ///   All implementations of <see cref="ITopicRepository"/> are expected to need the following logic at minimum.
  ///   Concrete implementations that are working directly with an underlying data source should prefer to instead derive from
  ///   the more opinionated <see cref="TopicRepository"/>, which provides more built-in business logic.
  /// </remarks>
  public abstract class ObservableTopicRepository : ITopicRepository {

    /*==========================================================================================================================
    | PRIVATE VARIABLES
    \-------------------------------------------------------------------------------------------------------------------------*/
    private                     EventHandler<TopicEventArgs>?                                   _deleteEvent;
    private                     EventHandler<TopicMoveEventArgs>?                               _moveEvent;
    private                     EventHandler<TopicRenameEventArgs>?                             _renameEvent;

    /*==========================================================================================================================
    | EVENT HANDLERS
    \-------------------------------------------------------------------------------------------------------------------------*/

    /// <inheritdoc />
    public virtual event EventHandler<TopicEventArgs>? DeleteEvent {
      add => _deleteEvent += value;
      remove => _deleteEvent -= value;
    }

    /// <inheritdoc />
    public virtual event EventHandler<TopicMoveEventArgs>? MoveEvent {
      add => _moveEvent += value;
      remove => _moveEvent -= value;
    }

    /// <inheritdoc />
    public virtual event EventHandler<TopicRenameEventArgs>? RenameEvent {
      add => _renameEvent += value;
      remove => _renameEvent -= value;
    }

    /*==========================================================================================================================
    | ON TOPIC DELETED
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Raises the <see cref="DeleteEvent"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Raising an event invokes the event handler through a delegate. For more information, see <seealso href="
    ///     https://docs.microsoft.com/en-us/dotnet/standard/events/">Handling and Raising Events</seealso>.
    ///   </para>
    ///   <para>
    ///     The <see cref="OnTopicDeleted(TopicEventArgs)"/> method also allows derived classes to handle the event without
    ///     attaching a delegate. This is the preferred technique for handling the event in a derived class.
    ///   </para>
    ///   <para>
    ///     When overriding the <see cref="OnTopicDeleted(TopicEventArgs)"/> method in a derived class, be sure to call the
    ///     base class's <see cref="OnTopicDeleted(TopicEventArgs)"/> method so that registered delegates receive the event.
    ///   </para>
    /// </remarks>
    /// <param name="args">An instance of the <see cref="TopicEventArgs"/> associated with the event.</param>
    protected virtual void OnTopicDeleted(TopicEventArgs args) => _deleteEvent?.Invoke(this, args);

    /*==========================================================================================================================
    | ON TOPIC MOVED
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Raises the <see cref="MoveEvent"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Raising an event invokes the event handler through a delegate. For more information, see <seealso href="
    ///     https://docs.microsoft.com/en-us/dotnet/standard/events/">Handling and Raising Events</seealso>.
    ///   </para>
    ///   <para>
    ///     The <see cref="OnTopicMoved(TopicMoveEventArgs)"/> method also allows derived classes to handle the event without
    ///     attaching a delegate. This is the preferred technique for handling the event in a derived class.
    ///   </para>
    ///   <para>
    ///     When overriding the <see cref="OnTopicMoved(TopicMoveEventArgs)"/> method in a derived class, be sure to call the
    ///     base class's <see cref="OnTopicMoved(TopicMoveEventArgs)"/> method so that registered delegates receive the event.
    ///   </para>
    /// </remarks>
    /// <param name="args">An instance of the <see cref="TopicMoveEventArgs"/> associated with the event.</param>
    protected virtual void OnTopicMoved(TopicMoveEventArgs args) => _moveEvent?.Invoke(this, args);

    /*==========================================================================================================================
    | ON TOPIC RENAMED
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Raises the <see cref="RenameEvent"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Raising an event invokes the event handler through a delegate. For more information, see <seealso href="
    ///     https://docs.microsoft.com/en-us/dotnet/standard/events/">Handling and Raising Events</seealso>.
    ///   </para>
    ///   <para>
    ///     The <see cref="OnTopicRenamed(TopicRenameEventArgs)"/> method also allows derived classes to handle the event
    ///     without attaching a delegate. This is the preferred technique for handling the event in a derived class.
    ///   </para>
    ///   <para>
    ///     When overriding the <see cref="OnTopicRenamed(TopicRenameEventArgs)"/> method in a derived class, be sure to call
    ///     the base class's <see cref="OnTopicRenamed(TopicRenameEventArgs)"/> method so that registered delegates receive the
    ///     event.
    ///   </para>
    /// </remarks>
    /// <param name="args">An instance of the <see cref="TopicRenameEventArgs"/> associated with the event.</param>
    protected virtual void OnTopicRenamed(TopicRenameEventArgs args) => _renameEvent?.Invoke(this, args);

    /*==========================================================================================================================
    | GET CONTENT TYPE DESCRIPTORS
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public abstract ContentTypeDescriptorCollection GetContentTypeDescriptors();

    /*==========================================================================================================================
    | METHOD: LOAD
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public abstract Topic? Load(int topicId, Topic? referenceTopic = null, bool isRecursive = true);

    /// <inheritdoc />
    public abstract Topic? Load(string? uniqueKey = null, Topic? referenceTopic = null, bool isRecursive = true);

    /// <inheritdoc />
    public abstract Topic? Load(Topic topic, DateTime version);

    /// <inheritdoc />
    public abstract Topic? Load(int topicId, DateTime version, Topic? referenceTopic = null);

    /*==========================================================================================================================
    | METHOD: REFRESH
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public abstract void Refresh(Topic referenceTopic, DateTime since);

    /*==========================================================================================================================
    | METHOD: ROLLBACK
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public abstract void Rollback(Topic topic, DateTime version);

    /*==========================================================================================================================
    | METHOD: SAVE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public abstract void Save(Topic topic, bool isRecursive = false);

    /*==========================================================================================================================
    | METHOD: MOVE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public abstract void Move(Topic topic, Topic target, Topic? sibling = null);

    /*==========================================================================================================================
    | METHOD: DELETE
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <inheritdoc />
    public abstract void Delete(Topic topic, bool isRecursive);

  } //Class
} //Namespace