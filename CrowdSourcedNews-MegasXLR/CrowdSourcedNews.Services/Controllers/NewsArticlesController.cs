﻿namespace CrowdSourcedNews.Services.Controllers
{
    using System;
    using System.Web.Http;
    using CrowdSourcedNews.Models;
    using CrowdSourcedNews.Repositories;
    using System.Net.Http;
    using CrowdSourcedNews.DataTransferObjects;
    using System.Net;
    using CrowdSourcedNews.Mappers;
    using System.Collections.Generic;
    using System.Linq;
    using Spring.Social.Dropbox.Api;
    using CrowdSourcedNews.Services.Utilities;

    public class NewsArticlesController : ApiController
    {
        private IRepository<NewsArticle> newsArticlesRepository;
        private DbUsersRepository usersRepository;
        private IRepository<Comment> commentsRepository;
        private static IDropbox dropbox = DropboxUtilities.CreateAndLoginDropBox();

        public NewsArticlesController(
            IRepository<NewsArticle> newsArticlesRepository,
            DbUsersRepository usersRepository,
            IRepository<Comment> commentsRepository)
        {
            this.newsArticlesRepository = newsArticlesRepository;
            this.usersRepository = usersRepository;
            this.commentsRepository = commentsRepository;
        }

        [HttpPost, ActionName("add")]
        public HttpResponseMessage PostNewsArticle(string sessionKey, [FromBody]NewsArticleModel newsArticle)
        {
            User user = null;
            try
            {
                user = this.usersRepository.GetBySessionKey(sessionKey);
            }
            catch (Exception)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid user!");
            }

            newsArticle.Author = user.Nickname;

            NewsArticle newsArticleEntity = null;
            try
            {
                newsArticleEntity = NewsArticlesMapper
                    .ToNewsArticleEntity(newsArticle, usersRepository, commentsRepository);
            }
            catch (Exception)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid news article model provided!");
            }

            newsArticle.ID = newsArticleEntity.ID;

            AddImages(newsArticle, newsArticleEntity);

            this.newsArticlesRepository.Add(newsArticleEntity);

            return Request.CreateResponse(HttpStatusCode.Created, newsArticle);
        }

        private static void AddImages(NewsArticleModel newsArticle, NewsArticle newsArticleEntity)
        {
            foreach (string imageUrl in newsArticle.ImagesUrls)
            {
                Entry uploadFileEntry = DropboxUtilities.UploadImage(imageUrl, dropbox, "New_Folder");
                DropboxLink imageLink = dropbox.GetShareableLinkAsync(uploadFileEntry.Path).Result;

                newsArticleEntity.ImagesUrls.Add(imageLink.Url);
            }
        }

        [HttpGet, ActionName("get")]
        public HttpResponseMessage GetNewsArticle(string sessionKey, int id)
        {
            User user = null;
            try
            {
                user = this.usersRepository.GetBySessionKey(sessionKey);
            }
            catch (Exception)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid user!");
            }

            NewsArticle newsArticle = this.newsArticlesRepository.Get(id);
            if (newsArticle == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "News article not found!");
            }

            NewsArticleModel newsArticleModel = null;
            try
            {
                newsArticleModel = NewsArticlesMapper.ToNewsArticleModel(newsArticle);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, newsArticleModel);
            return response;
        }

        [HttpGet, ActionName("get")]
        public HttpResponseMessage GetAll(string sessionKey)
        {
            User user = null;
            try
            {
                user = this.usersRepository.GetBySessionKey(sessionKey);
            }
            catch (Exception)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid user!");
            }

            ICollection<NewsArticleDetails> newsArticles = new List<NewsArticleDetails>();
            IQueryable<NewsArticle> newsArticlesEntities = this.newsArticlesRepository.GetAll();
            foreach (var newsArticle in newsArticlesEntities)
            {
                newsArticles.Add(NewsArticlesMapper.ToNewsArticleDetails(newsArticle));
            }

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, newsArticles);
            return response;
        }

        [HttpPut, ActionName("edit")]
        public HttpResponseMessage EditNewsArticle(string sessionKey, int id, NewsArticleModel newsArticle)
        {
            User user = null;
            try
            {
                user = this.usersRepository.GetBySessionKey(sessionKey);
            }
            catch (Exception)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid user!");
            }

            if (id != newsArticle.ID)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }

            NewsArticle updatedNewsArticle = null;
            try
            {
                updatedNewsArticle =
                    NewsArticlesMapper.ToNewsArticleEntity(newsArticle, usersRepository, commentsRepository);
            }
            catch (Exception)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid news article model provided!");
            }

            this.newsArticlesRepository.Update(id, updatedNewsArticle);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpDelete, ActionName("remove")]
        public HttpResponseMessage RemoveNewsArticle(string sessionKey, int id)
        {
            User user = null;
            try
            {
                user = this.usersRepository.GetBySessionKey(sessionKey);
            }
            catch (Exception)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid user!");
            }

            if (this.newsArticlesRepository.Delete(id))
            {
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "News article not found!");
            }
        }
    }
}
