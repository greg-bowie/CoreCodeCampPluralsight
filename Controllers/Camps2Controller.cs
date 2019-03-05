using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CoreCodeCamp.Controllers
{
    [Route("api/v{version:apiVersion}/camps")]
    [ApiVersion("2.0")]
    [ApiController]
    public class Camps2Controller : ControllerBase
    {
        private readonly ICampRepository TheRepository;
        private readonly IMapper Mapper;
        private readonly LinkGenerator LinkGenerator;
        public Camps2Controller(ICampRepository repository, IMapper mapper, LinkGenerator linkGenerator)
        {
            TheRepository = repository;
            Mapper = mapper;
            LinkGenerator = linkGenerator;
        }

        [HttpGet]
        public async Task<IActionResult> GetCamps(bool includeTalks = false)
        {
            try
            {
                var results = await TheRepository.GetAllCampsAsync(includeTalks);
                var result = new
                {
                    Count = results.Count(),
                    Results = Mapper.Map<CampModel[]>(results)
                };
                return Ok(result);
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failed");
            }
        }

        [HttpGet("{moniker}")]
        public async Task<ActionResult<CampModel>> Get(string moniker)
        {
            try
            {
                var result = await TheRepository.GetCampAsync(moniker);

                if (result is null)
                {
                    return NotFound();
                }

                return Mapper.Map<CampModel>(result);
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failed");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<CampModel[]>> SearchByDate(DateTime theDate, bool includeTalks = false)
        {
            try
            {
                var results = await TheRepository.GetAllCampsByEventDate(theDate, includeTalks);

                if (!results.Any()) return NotFound();

                return Mapper.Map<CampModel[]>(results);
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failed");
            }
        }

        public async Task<ActionResult<CampModel>> Post(CampModel model)
        {
            try
            {
                var existing = await TheRepository.GetCampAsync(model.Moniker);
                if (existing != null)
                {
                    return BadRequest("Moniker in use");
                }

                var location = LinkGenerator.GetPathByAction("Get", "Camps", new { moniker = model.Moniker });

                if (string.IsNullOrWhiteSpace(location))
                {
                    return BadRequest("Could not use current moniker");
                }

                // Create a new camp
                var camp = Mapper.Map<Camp>(model);
                TheRepository.Add(camp);
                if (await TheRepository.SaveChangesAsync())
                {
                    return Created(location, Mapper.Map<CampModel>(camp));
                }
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failed");
            }

            return BadRequest();
        }

        [HttpPut("{moniker}")]
        public async Task<ActionResult<CampModel>> Put(string moniker, CampModel model)
        {
            try
            {
                var oldCamp = await TheRepository.GetCampAsync(moniker);

                if (oldCamp is null)
                {
                    return NotFound("Could not find camp with moniker of ${moniker}");
                }

                Mapper.Map(model, oldCamp);

                if (await TheRepository.SaveChangesAsync())
                {
                    return Mapper.Map<CampModel>(oldCamp);
                }

            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failed");
            }
            return BadRequest();
        }

        [HttpDelete("{moniker}")]
        public async Task<ActionResult> Delete(string moniker)
        {
            try
            {
                var oldCamp = await TheRepository.GetCampAsync(moniker);

                if (oldCamp is null)
                {
                    return NotFound("Could not find camp with moniker of ${moniker}");
                }

                TheRepository.Delete(oldCamp);

                if (await TheRepository.SaveChangesAsync())
                {
                    return Ok();
                }
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database failed");
            }
            return BadRequest("Failed to delete the camp");
        }
    }
}