﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okurdostu.Data;
using Okurdostu.Web.Base;
using Okurdostu.Web.Filters;
using Okurdostu.Web.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Okurdostu.Web.Controllers.Api
{
    public class CommentsController : SecureApiController
    {
        //api/comments/{Id} -- get single comment
        [HttpGet("{Id}")]
        public async Task<IActionResult> GetSingle(Guid Id)
        {
            JsonReturnModel jsonReturnModel = new JsonReturnModel();
            //when user does reply a comment or wants to edit their comment, it works to view comment that will be edited or replied.
            var RequestedComment = await Context.NeedComment.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == Id && !x.IsRemoved);

            if (RequestedComment != null)
            {
                jsonReturnModel.Data = new { comment = RequestedComment.Comment, username = RequestedComment.User.Username, fullname = RequestedComment.User.FullName };
                return Succes(jsonReturnModel);
            }
            else
            {
                jsonReturnModel.Code = 200;
                jsonReturnModel.Message = "Böyle bir yorum yok";
                return Error(jsonReturnModel);
            }
        }

        //api/comments -- add a new comment or add a reply comment
        public class CommentModel
        {
            public Guid? NeedId { get; set; }
            public Guid? RelatedCommentId { get; set; }

            [Required(ErrorMessage = "Bir şeyler yazmalısın")]
            [MaxLength(100, ErrorMessage = "En fazla 100 karakter")]
            [DataType(DataType.MultilineText)]
            public string Comment { get; set; }
        }

        [ServiceFilter(typeof(ConfirmedEmailFilter))]
        [HttpPost("")]
        public async Task<IActionResult> PostAdd(CommentModel model) //doing comment or reply
        {
            JsonReturnModel jsonReturnModel = new JsonReturnModel();

            if (!ModelState.IsValid)
            {
                if (model.Comment == null)
                {
                    jsonReturnModel.Message = "Bir şeyler yazmalısın";
                }
                else if (model.Comment.Length > 100)
                {
                    jsonReturnModel.Message = "En fazla 100 karakter";
                }
                jsonReturnModel.InternalMessage = "Comment is required";

                return Error(jsonReturnModel);
            }

            if (model.NeedId == null && model.RelatedCommentId == null || model.NeedId == Guid.Empty && model.RelatedCommentId == null)
            {
                jsonReturnModel.Message = "Yorum yapmak istediğiniz içeriği veya cevabı kontrol edin";
                jsonReturnModel.InternalMessage = "If you doing main comment on a need, NeedId is required. If you try to reply, RelatedCommentId is required";

                return Error(jsonReturnModel);
            }

            if (model.NeedId != Guid.Empty && model.NeedId != null) // add new comment
            {
                var commentedNeed = await Context.Need.AnyAsync(x => x.Id == model.NeedId && !x.IsRemoved && x.IsConfirmed);

                if (commentedNeed)
                {
                    var NewComment = new NeedComment
                    {
                        Comment = model.Comment,
                        NeedId = (Guid)model.NeedId,
                        UserId = Guid.Parse(User.Identity.GetUserId())
                    };

                    await Context.AddAsync(NewComment);
                    var result = await Context.SaveChangesAsync();

                    if (result > 0)
                    {
                        jsonReturnModel.Data = NewComment.Id;
                        return Succes(jsonReturnModel);
                    }
                    else
                    {
                        jsonReturnModel.Message = "Yorumunuz kaydolmadı";
                        return Error(jsonReturnModel);
                    }
                }
                else
                {
                    jsonReturnModel.Message = "Tartışmanın başlatılacağı kampanya yok veya burada tartışma başlatılamaz";
                    return Error(jsonReturnModel);
                }
            }
            else  //[reply] add relational comment
            {
                var repliedComment = await Context.NeedComment.Include(comment => comment.Need).FirstOrDefaultAsync(x => x.Id == model.RelatedCommentId && !x.IsRemoved && !x.Need.IsRemoved && x.Need.IsConfirmed);

                if (repliedComment != null)
                {
                    var NewReply = new NeedComment
                    {
                        Comment = model.Comment,
                        UserId = Guid.Parse(User.Identity.GetUserId()),
                        NeedId = repliedComment.NeedId,
                        RelatedCommentId = repliedComment.Id
                    };

                    await Context.AddAsync(NewReply);
                    var result = Context.SaveChanges();

                    if (result > 0)
                    {
                        jsonReturnModel.Data = NewReply.Id;
                        jsonReturnModel.Message = "Cevapladınız";
                        return Succes(jsonReturnModel);
                    }
                    else
                    {
                        jsonReturnModel.Code = 200;
                        jsonReturnModel.Message = "Başaramadık, ne olduğunu bilmiyoruz";
                        return Error(jsonReturnModel);
                    }
                }
                else
                {
                    jsonReturnModel.Code = 200;
                    jsonReturnModel.Message = "Cevaplanacak yorum yok, silinmiş veya burada cevap verilemez";
                    return Error(jsonReturnModel);
                }
            }
        }

        //api/comments/remove/{Id}
        [HttpPatch("remove/{Id}")]
        public async Task<IActionResult> PatchRemove(Guid Id)
        {
            JsonReturnModel jsonReturnModel = new JsonReturnModel();
            if (Id == null || Id == Guid.Empty)
            {
                jsonReturnModel.Message = "Silmek için yorum seçmediniz";
                jsonReturnModel.InternalMessage = "Id is required";
                return Error(jsonReturnModel);
            }

            var DeletedComment = await Context.NeedComment.FirstOrDefaultAsync(x => x.Id == Id && !x.IsRemoved && x.UserId == Guid.Parse(User.Identity.GetUserId()));

            if (DeletedComment != null)
            {
                DeletedComment.IsRemoved = true;
                DeletedComment.UserId = null; // hangi user'ın bu veriyi girdiği boş bırakılmalı
                DeletedComment.Comment = ""; // aynı şekilde içerik yok edilmeli
                await Context.SaveChangesAsync();

                jsonReturnModel.Message = "Yorumunuz silindi";
                return Succes(jsonReturnModel);
            }
            else
            {
                jsonReturnModel.Message = "Silmeye çalıştığınız yorum yok";
                return Error(jsonReturnModel);
            }
        }

        //api/comments/{Id} -- edit
        public class EditCommentModel
        {
            [Required]
            public Guid Id { get; set; }

            [Required(ErrorMessage = "Bir şeyler yazmalısın")]
            [MaxLength(100, ErrorMessage = "En fazla 100 karakter")]
            [DataType(DataType.MultilineText)]
            public string Comment { get; set; }
        }
        [HttpPatch("{Id}")]
        public async Task<IActionResult> PatchEdit(EditCommentModel model)
        {
            JsonReturnModel jsonReturnModel = new JsonReturnModel();
            var EditedComment = await Context.NeedComment.FirstOrDefaultAsync(x => x.Id == model.Id && !x.IsRemoved && x.UserId == Guid.Parse(User.Identity.GetUserId()));

            if (!ModelState.IsValid)
            {
                if (model.Comment == null)
                {
                    jsonReturnModel.Message = "Bir şeyler yazmalısın";
                }
                else if (model.Comment.Length > 100)
                {
                    jsonReturnModel.Message = "En fazla 100 karakter";
                }

                jsonReturnModel.InternalMessage = "Id and comment are required";

                return Error(jsonReturnModel);
            }

            if (EditedComment != null)
            {
                if (EditedComment.Comment != model.Comment)
                {
                    EditedComment.Comment = model.Comment;
                    await Context.SaveChangesAsync();
                    jsonReturnModel.Message = "Yorum içeriğiniz düzenlendi";
                    return Succes(jsonReturnModel);
                }
                else
                {
                    jsonReturnModel.Message = "Aynı içerik ile düzenlemeye çalıştınız";
                    return Error(jsonReturnModel);
                }
            }
            else
            {
                jsonReturnModel.Message = "Düzenlemeye çalıştığınız yorum yok";
                return Error(jsonReturnModel);
            }
        }
    }
}