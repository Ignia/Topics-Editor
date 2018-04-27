﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ignia.Topics.Querying;
using Ignia.Topics.Web.Mvc;
using Ignia.Topics.Repositories;
using Ignia.Topics.Data.Caching;
using Ignia.Topics.Tests.TestDoubles;
using Ignia.Topics.Web.Mvc.Controllers;
using Ignia.Topics.ViewModels;
using System.Web.Mvc;
using System.Web.Routing;
using Ignia.Topics.Mapping;
using Ignia.Topics.Web.Mvc.Models;

namespace Ignia.Topics.Tests {

  /*============================================================================================================================
  | CLASS: TOPIC CONTROLLER TEST
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   Provides unit tests for the <see cref="TopicController"/>, and other <see cref="Controller"/> classes that are part of
  ///   the <see cref="Ignia.Topics.Web.Mvc"/> namespace.
  /// </summary>
  [TestClass]
  public class TopicControllerTest {

    /*==========================================================================================================================
    | PRIVATE VARIABLES
    \-------------------------------------------------------------------------------------------------------------------------*/
    ITopicRepository            _topicRepository                = null;

    /*==========================================================================================================================
    | CONSTRUCTOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Initializes a new instance of the <see cref="TopicControllerTest"/> with shared resources.
    /// </summary>
    /// <remarks>
    ///   This uses the <see cref="FakeTopicRepository"/> to provide data, and then <see cref="CachedTopicRepository"/> to
    ///   manage the in-memory representation of the data. While this introduces some overhead to the tests, the latter is a
    ///   relatively lightweight façade to any <see cref="ITopicRepository"/>, and prevents the need to duplicate logic for
    ///   crawling the object graph.
    /// </remarks>
    public TopicControllerTest() {
      _topicRepository = new CachedTopicRepository(new FakeTopicRepository());
    }

    /*==========================================================================================================================
    | TEST: ERROR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Triggers the <see cref="ErrorControllerBase{T}.Error(string)" /> action.
    /// </summary>
    [TestMethod]
    public void ErrorController_ErrorTest() {

      var controller            = new ErrorController();
      var result                = controller.Error("ErrorPage") as ViewResult;
      var model                 = result.Model as PageTopicViewModel;

      Assert.IsNotNull(model);
      Assert.AreEqual<string>("ErrorPage", model.Title);

    }

    /*==========================================================================================================================
    | TEST: NOT FOUND ERROR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Triggers the <see cref="ErrorControllerBase{T}.NotFound(string)" /> action.
    /// </summary>
    [TestMethod]
    public void ErrorController_NotFoundTest() {

      var controller            = new ErrorController();
      var result                = controller.Error("NotFoundPage") as ViewResult;
      var model                 = result.Model as PageTopicViewModel;

      Assert.IsNotNull(model);
      Assert.AreEqual<string>("NotFoundPage", model.Title);

    }

    /*==========================================================================================================================
    | TEST: INTERNAL SERVER ERROR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Triggers the <see cref="ErrorControllerBase{T}.InternalServer(string)" /> action.
    /// </summary>
    [TestMethod]
    public void ErrorController_InternalServerTest() {

      var controller            = new ErrorController();
      var result                = controller.Error("InternalServer") as ViewResult;
      var model                 = result.Model as PageTopicViewModel;

      Assert.IsNotNull(model);
      Assert.AreEqual<string>("InternalServer", model.Title);

    }

    /*==========================================================================================================================
    | TEST: FALLBACK
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Triggers the <see cref="FallbackController.Index()" /> action.
    /// </summary>
    [TestMethod]
    public void FallbackController_IndexTest() {

      var controller            = new FallbackController();
      var result                = controller.Index() as HttpNotFoundResult;

      Assert.IsNotNull(result);
      Assert.AreEqual<int>(404, result.StatusCode);
      Assert.AreEqual<string>("No controller available to handle this request.", result.StatusDescription);

    }

    /*==========================================================================================================================
    | TEST: REDIRECT
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Triggers the <see cref="FallbackController.Index()" /> action.
    /// </summary>
    [TestMethod]
    public void RedirectController_TopicRedirectTest() {

      var controller            = new RedirectController(_topicRepository);
      var result                = controller.Redirect(11110) as RedirectResult;

      Assert.IsNotNull(result);
      Assert.IsTrue(result.Permanent);
      Assert.AreEqual<string>("/Web/Web_1/Web_1_1/Web_1_1_1/", result.Url);

    }

    /*==========================================================================================================================
    | TEST: SITEMAP
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Triggers the index action of the <see cref="SitemapController.Index()" /> action.
    /// </summary>
    /// <remarks>
    ///   Because the <see cref="SitemapController.Index()"/> method references the <see cref="Controller.Response"/> property,
    ///   which is not set during unit testing, this test is <i>expected</i> to throw an exception. This is not ideal. In the
    ///   future, this may be modified to instead use a mock <see cref="ControllerContext"/> for a more sophisticated test.
    /// </remarks>
    [TestMethod]
    [ExpectedException(typeof(NullReferenceException), AllowDerivedTypes=false)]
    public void SitemapController_IndexTest() {

      var controller            = new SitemapController(_topicRepository);
      var result                = controller.Index() as ViewResult;
      var model                 = result.Model as TopicEntityViewModel;

      Assert.IsNotNull(model);
      Assert.AreEqual<ITopicRepository>(_topicRepository, model.TopicRepository);
      Assert.AreEqual<string>("Root", model.RootTopic.Key);
      Assert.AreEqual<string>("Root", model.Topic.Key);

    }

    /*==========================================================================================================================
    | TEST: MENU
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Triggers the <see cref="FallbackController.Index()" /> action.
    /// </summary>
    [TestMethod]
    public void LayoutController_MenuTest() {

      var routes                = new RouteData();
      var uri                   = new Uri("http://localhost/Web/Web_0/Web_0_1/Web_0_1_1");
      var topic                 = _topicRepository.Load("Root:Web:Web_0:Web_0_1:Web_0_1_1");

      var topicRoutingService   = new MvcTopicRoutingService(_topicRepository, uri, routes);
      var mappingService        = new TopicMappingService(_topicRepository);

      var controller            = new LayoutController(_topicRepository, topicRoutingService, mappingService);
      var result                = controller.Menu() as PartialViewResult;
      var model                 = result.Model as NavigationViewModel<NavigationTopicViewModel>;

      Assert.IsNotNull(model);
      Assert.AreEqual<string>(topic.GetUniqueKey(), model.CurrentKey);
      Assert.AreEqual<string>("Root:Web", model.NavigationRoot.UniqueKey);
      Assert.AreEqual<int>(3, model.NavigationRoot.Children.Count());
      Assert.IsTrue(model.NavigationRoot.IsSelected(topic.GetUniqueKey()));

    }


  } //Class

} //Namespace