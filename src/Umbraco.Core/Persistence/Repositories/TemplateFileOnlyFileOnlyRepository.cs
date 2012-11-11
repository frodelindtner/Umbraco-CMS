﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Umbraco.Core.Configuration;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.UnitOfWork;

namespace Umbraco.Core.Persistence.Repositories
{
    /// <summary>
    /// Represents the Template Repository
    /// </summary>
    internal class TemplateFileOnlyFileOnlyRepository : FileRepository<string, Template>, ITemplateFileOnlyRepository
    {
        private readonly IFileSystem _viewsFileSystem;
        
        public TemplateFileOnlyFileOnlyRepository(IUnitOfWork work)
            : base(work, FileSystemProviderManager.Current.GetFileSystemProvider("masterpages"))
        {
            _viewsFileSystem = FileSystemProviderManager.Current.GetFileSystemProvider("views");
        }

        #region Overrides of FileRepository<string,Template>

        public override Template Get(string id)
        {
            string masterpageName = string.Concat(id, ".master");
            string viewName = string.Concat(id, ".cshtml");
            if (!FileSystem.FileExists(masterpageName) && !_viewsFileSystem.FileExists(viewName))
            {
                throw new Exception(string.Format("The file with alias: '{0}' was not found", id));
            }

            string content = string.Empty;
            string path = string.Empty;
            DateTime created = new DateTime();
            DateTime updated = new DateTime();
            string name = string.Empty;

            if (FileSystem.FileExists(masterpageName))
            {
                using (var stream = FileSystem.OpenFile(masterpageName))
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.Position = 0;
                    stream.Read(bytes, 0, (int) stream.Length);
                    content = Encoding.UTF8.GetString(bytes);
                }

                path = FileSystem.GetRelativePath(masterpageName);
                created = FileSystem.GetCreated(path).UtcDateTime;
                updated = FileSystem.GetLastModified(path).UtcDateTime;
                name = new FileInfo(path).Name;
            }
            else
            {
                using (var stream = _viewsFileSystem.OpenFile(viewName))
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.Position = 0;
                    stream.Read(bytes, 0, (int) stream.Length);
                    content = Encoding.UTF8.GetString(bytes);
                }

                path = _viewsFileSystem.GetRelativePath(viewName);
                created = FileSystem.GetCreated(path).UtcDateTime;
                updated = FileSystem.GetLastModified(path).UtcDateTime;
                name = new FileInfo(path).Name;
            }

            var template = new Template(path)
                               {
                                   Content = content,
                                   Key = name.EncodeAsGuid(),
                                   CreateDate = created,
                                   UpdateDate = updated
                               };

            template.ResetDirtyProperties();

            return template;
        }

        public override IEnumerable<Template> GetAll(params string[] ids)
        {
            if (ids.Any())
            {
                foreach (var id in ids)
                {
                    yield return Get(id);
                }
            }
            else
            {
                var files = FileSystem.GetFiles("", "*").ToList();
                files.AddRange(_viewsFileSystem.GetFiles("", "*"));
                foreach (var file in files)
                {
                    yield return Get(file);
                }
            }
        }

        public override bool Exists(string id)
        {
            return FileSystem.FileExists(id) || _viewsFileSystem.FileExists(id);
        }

        #endregion

        #region Abstract IUnitOfWorkRepository Methods

        protected override void PersistNewItem(Template entity)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(entity.Content));
            if (UmbracoSettings.DefaultRenderingEngine == RenderingEngine.Mvc)
            {
                _viewsFileSystem.AddFile(entity.Name, stream, true);
            }
            else
            {
                FileSystem.AddFile(entity.Name, stream, true);
            }

            entity.ResetDirtyProperties();
        }

        protected override void PersistUpdatedItem(Template entity)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(entity.Content));
            if (UmbracoSettings.DefaultRenderingEngine == RenderingEngine.Mvc)
            {
                _viewsFileSystem.AddFile(entity.Name, stream, true);
            }
            else
            {
                FileSystem.AddFile(entity.Name, stream, true);
            }

            entity.ResetDirtyProperties();
        }

        protected override void PersistDeletedItem(Template entity)
        {
            //Check for file under the Masterpages filesystem
            if (FileSystem.FileExists(entity.Name))
            {
                FileSystem.DeleteFile(entity.Name);
            }
            else if (FileSystem.FileExists(entity.Path))
            {
                FileSystem.DeleteFile(entity.Path);
            }

            //Check for file under the Views/Mvc filesystem
            if (_viewsFileSystem.FileExists(entity.Name))
            {
                _viewsFileSystem.DeleteFile(entity.Name);
            }
            else if (_viewsFileSystem.FileExists(entity.Path))
            {
                _viewsFileSystem.DeleteFile(entity.Path);
            }
        }

        #endregion
    }
}