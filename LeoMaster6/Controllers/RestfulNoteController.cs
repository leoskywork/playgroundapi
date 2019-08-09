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
    public class RestfulNoteController : BaseController
    {
        public IHttpActionResult Get()
        {
            return Ok("Restful note get success");
        }

        //!! the parameter name must match 'id' when use default routing, otherwise api call(.../restfulNote/xxx) will NOT work
        //  - which is because the placeholder variable in the default routing template is {id}, ref WebApiConfig.cs
        [HttpDelete]
        public IHttpActionResult Clipboard(string id)
        {
            if (!ModelState.IsValid)
            {

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

            var notes = ReadLskjson<Guid, DtoClipboardItem>(path, CollectLskjsonLine);
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
    }
}