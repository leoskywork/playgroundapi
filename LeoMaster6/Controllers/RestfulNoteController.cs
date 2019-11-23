using LeoMaster6.Common;
using LeoMaster6.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace LeoMaster6.Controllers
{
    [RoutePrefix("v2/note")]
    public class RestfulNoteController : BaseController
    {

        public class PutBody
        {
            public string Data { get; set; }
        }

        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok("Restful note get success");
        }

        //!! the parameter name must match 'id' when use default routing, otherwise api call(.../restfulNote/xxx) will NOT work
        //  - which is because the placeholder variable in the default routing template is {id}, ref WebApiConfig.cs
        //match by route attribute: DELETE v2/note/xxx-xyz
        //  - once you set [Route] you must also set [RoutePrefix] on the class, and use them combined
        [HttpDelete]
        [Route("{id:guid}")]
        public IHttpActionResult Clipboard(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToString());
            }

            if (id == null) throw new ArgumentNullException(nameof(id));

            var uid = Guid.Parse(id);
            var createdAt = GetOriginCreatedAt(uid);

            if (!createdAt.HasValue)
            {
                return DtoResultV5.Success(Json, "already deleted");
            }

            string path = GetFullClipboardDataPath(createdAt.Value); //ensure orig item and (soft)deleted item in the same file

            if (!File.Exists(path)) return DtoResultV5.Success(Json, "no data - data source not exist");

            var notes = ReadLskjson<Guid, DtoClipboardItem>(path, CollectLskjsonLineClipboard);
            var foundNote = notes.FirstOrDefault(n => n.Uid == uid);

            if (foundNote == null) return DtoResultV5.Success(Json, "already deleted");

            foundNote.HasDeleted = true;
            //todo - replace with real UserId(get by session id)
            foundNote.DeletedBy = Constants.DevDeleteUserId + DateTime.Now.ToString("dd");
            foundNote.DeletedAt = DateTime.Now;

            //need replace a line in the file - seems there is no way to just rewrite one line, have to re-write entire file
            // - https://stackoverflow.com/questions/1971008/edit-a-specific-line-of-a-text-file-in-c-sharp
            // - https://stackoverflow.com/questions/13509532/how-to-find-and-replace-text-in-a-file-with-c-sharp
            var backupPath = path.Replace(Constants.LskjsonPrefix, Constants.LskjsonPrefix + DateTime.Now.ToString("dd-HHmmss-"));
            File.Move(path, backupPath);
            var success = AppendNoteToFile(path, notes.ToArray());

            if (success)
            {
                File.Delete(backupPath);
                return DtoResultV5.Success(Json, "Data deleted");
            }
            else
            {
                File.Move(backupPath, path);
                return DtoResultV5.Fail(Json, "Failed to delete data");
            }
        }

        //match by route template: DELETE restfulNote/xxx-xyz
        [HttpDelete]
        public IHttpActionResult CleanClipboard(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToString());
            }

            if (id == null) throw new ArgumentNullException(nameof(id));

            var uid = Guid.Parse(id);
            var createdAt = GetOriginCreatedAt(uid);

            if (!createdAt.HasValue)
            {
                return DtoResultV5.Success(Json, "Data already deleted");
            }

            string path = GetFullClipboardDataPath(createdAt.Value); //ensure orig item and (soft)deleted item in the same file

            if (!File.Exists(path)) return DtoResultV5.Success(Json, "no data");

            var notes = ReadLskjson<Guid, DtoClipboardItem>(path, CollectLskjsonLineClipboard);
            var foundNote = notes.FirstOrDefault(n => n.Uid == uid);

            if (foundNote == null) return DtoResultV5.Success(Json, "already deleted");

            foundNote.HasDeleted = true;
            //todo - replace with real UserId(get by session id)
            foundNote.DeletedBy = Constants.DevDeleteUserId + DateTime.Now.ToString("dd");
            foundNote.DeletedAt = DateTime.Now;

            //need replace a line in the file - seems there is no way to just rewrite one line, have to re-write entire file
            // - https://stackoverflow.com/questions/1971008/edit-a-specific-line-of-a-text-file-in-c-sharp
            // - https://stackoverflow.com/questions/13509532/how-to-find-and-replace-text-in-a-file-with-c-sharp
            var backupPath = path.Replace(Constants.LskjsonPrefix, Constants.LskjsonPrefix + DateTime.Now.ToString("dd-HHmmss-"));
            File.Move(path, backupPath);
            var success = AppendNoteToFile(path, notes.ToArray());

            if (success)
            {
                File.Delete(backupPath);
                return DtoResultV5.Success(Json, "Data deleted");
            }
            else
            {
                File.Move(backupPath, path);
                return DtoResultV5.Fail(Json, "Failed to delete data");
            }
        }

        //PUT v2/note/xxx-xyz { "Data": "xxx new data" }
        [HttpPut]
        [Route("{id:guid}")]
        public IHttpActionResult Clipboard(string id, [FromBody]PutBody putBody)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (putBody == null) throw new ArgumentNullException(nameof(putBody));

            var inputUid = Guid.Parse(id);

            //todo improve this, hard coded as dev session id for now
            if (!CheckHeaderSession())
            {
                //return Unauthorized();
            }

            var createdAt = GetOriginCreatedAt(inputUid);

            if (!createdAt.HasValue)
            {
                throw new InvalidOperationException("Entry not found, may already deleted or never exist, please reload to latest then edit");
            }

            var path = GetFullClipboardDataPath(createdAt.Value); //ensure orig item and updated item in the same file

            if (!File.Exists(path)) return DtoResultV5.Success(this.Json, "no data");

            //perf - O(n) is 2n here, can be optimized to n
            var notes = ReadLskjson<Guid, DtoClipboardItem>(path, CollectLskjsonLineClipboard);
            var foundNote = notes.FirstOrDefault(n => n.Uid == inputUid);
            var foundChild = notes.FirstOrDefault(n => n.ParentUid == inputUid);

            //ensure the relation is a chain, not a tree
            if (foundChild != null)
            {
                throw new InvalidOperationException("Data already changed, please reload to latest then edit");
            }

            if (foundNote != null)
            {
                var newNote = Utility.DeepClone(foundNote);

                newNote.Uid = Guid.NewGuid(); //reset
                newNote.Data = putBody.Data;

                //todo - replace with real UserId(get by session id)
                newNote.HasUpdated = true;
                newNote.LastUpdatedBy = Constants.DevUpdateUserId + DateTime.Now.ToString("dd");
                newNote.LastUpdatedAt = DateTime.Now;
                newNote.ParentUid = foundNote.Uid;

                AppendNoteToFile(path, newNote);
                AppendObjectToFile(GetFullClipboardIndexPath(newNote.CreatedAt), DtoLskjsonIndex.From(newNote));

                return DtoResultV5.Success(Json, default(object), "Data updated");
            }
            else
            {
                return DtoResultV5.Fail(Json, "The data you want to update may have been deleted");
            }
        }

    }
}