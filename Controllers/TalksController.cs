using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreCodeCamp.Controllers
{
    [ApiController]
    [Route("api/camps/{moniker}/talks")]
    public class TalksController : ControllerBase
    {
        private readonly ICampRepository Repository;
        private readonly IMapper Mapper;
        private readonly LinkGenerator LinkGenerator;

        public TalksController(ICampRepository repository, IMapper mapper, LinkGenerator linkGenerator)
        {
            Repository = repository;
            Mapper = mapper;
            LinkGenerator = linkGenerator;
        }

        [HttpGet]
        public async Task<ActionResult<TalkModel[]>> Get(string moniker)
        {
            try
            {
                var talks = await Repository.GetTalksByMonikerAsync(moniker);

                return Mapper.Map<TalkModel[]>(talks);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TalkModel[]>> Get(string moniker, int id)
        {
            try
            {
                var talks = await Repository.GetTalkByMonikerAsync(moniker, id, true);
                if (talks is null) return NotFound("Could not find talk");
                return Mapper.Map<TalkModel[]>(talks);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
            }
        }

        [HttpPost]
        public async Task<ActionResult<TalkModel>> Post(string moniker, TalkModel model)
        {
            try
            {
                var camp = await Repository.GetCampAsync(moniker);
                if (camp is null) return BadRequest("Camp does not exist");
                if (model.Speaker is null) return BadRequest("Speaker does not exist");

                var talk = Mapper.Map<Talk>(model);
                talk.Camp = camp;
                var speaker = await Repository.GetSpeakerAsync(talk.Speaker.SpeakerId);
                if (speaker is null) return BadRequest("Speaker could not be found");
                talk.Speaker = speaker;
                Repository.Add(talk);

                if (await Repository.SaveChangesAsync())
                {
                    var url = LinkGenerator.GetPathByAction(
                        HttpContext,
                        "Get",
                        values: new { moniker, id = talk.TalkId });

                    return Created(url, Mapper.Map<TalkModel>(talk));
                }
                else
                {
                    return BadRequest("Failed to save new talk");
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TalkModel>> Put(string moniker, int id, TalkModel model)
        {
            try
            {
                var talk = await Repository.GetTalkByMonikerAsync(moniker, id, true);

                if (talk is null) return NotFound("Couldn't find the talk");

                Mapper.Map(model, talk);

                if (model.Speaker != null)
                {
                    var speaker = await Repository.GetSpeakerAsync(model.Speaker.SpeakerId);
                    if (speaker != null)
                    {
                        talk.Speaker = speaker;
                    }
                }

                if (await Repository.SaveChangesAsync())
                {
                    return Mapper.Map<TalkModel>(talk);
                }
                else
                {
                    return BadRequest("Failed to update database");
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(string moniker, int id)
        {
            try
            {
                var talk = await Repository.GetTalkByMonikerAsync(moniker, id);
                if (talk is null) return NotFound("Failed to find the talk to delete");

                Repository.Delete(talk);

                if (await Repository.SaveChangesAsync())
                {
                    return Ok();
                }
                else
                {
                    return BadRequest("Failed to delete talk");
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
            }
        }
    }
}
