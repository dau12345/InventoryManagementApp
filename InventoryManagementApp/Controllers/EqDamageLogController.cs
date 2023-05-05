﻿using AutoMapper;
using InventoryManagementApp.Data.Interfaces;
using InventoryManagementApp.Data.Models;
using InventoryManagementApp.Data.Repository;
using InventoryManagementApp.Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EqDamageLogController : Controller
    {
        private readonly IEqDamageLogRepository _eqDamageLogRepository;
        private readonly IMapper _mapper;

        public EqDamageLogController(IEqDamageLogRepository eqDamageLogRepository,IMapper mapper)
        {
            this._eqDamageLogRepository = eqDamageLogRepository;
            this._mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<EqDamageLog>))]
        public IActionResult GetEqDamageLog()
        {
            var eqdamagelogs = _mapper.Map<List<EqDamageLogVM>>(_eqDamageLogRepository.GetEqDamageLogs());

            foreach (EqDamageLogVM us in eqdamagelogs)
            {
                us.DetailEqDamageLogs = _mapper.Map<List<DetailEqDamageLogVM>>(_eqDamageLogRepository.GetDetailEqDamageLogs(us.EqDamageLogID));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok(eqdamagelogs);
        }

        [HttpGet("{eqdamagelogID}")]
        [ProducesResponseType(200, Type = typeof(EqDamageLog))]
        [ProducesResponseType(400)]
        public IActionResult GetEqDamageLog(int eqdamagelogID)
        {
            if (!_eqDamageLogRepository.EqDamageLogExists(eqdamagelogID))
            {
                return NotFound();
            }

            var eqdamagelog = _mapper.Map<EqDamageLogVM>(_eqDamageLogRepository.GetEqDamageLogById(eqdamagelogID));
            eqdamagelog.DetailEqDamageLogs = _mapper.Map<List<DetailEqDamageLogVM>>(_eqDamageLogRepository.GetDetailEqDamageLogs(eqdamagelogID));


            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok(eqdamagelog);
        }

        [HttpPost]
        public IActionResult CreateEqDamageLog(EqDamageLogVM eqDamageLogCreate)
        {
            if (eqDamageLogCreate == null || eqDamageLogCreate.DetailEqDamageLogs == null)
            {
                return BadRequest(ModelState);
            }

            var eqDamageLogMap = _mapper.Map<EqDamageLog>(eqDamageLogCreate);

            if (!_eqDamageLogRepository.CreateEqDamageLog(eqDamageLogMap))
            {
                ModelState.AddModelError("", "Something went wrong while saving");
                return StatusCode(500, ModelState);
            }

            List<DetailEqDamageLog> detaiEqDamageLogMaps = new List<DetailEqDamageLog>();

            foreach (DetailEqDamageLogVM item in eqDamageLogCreate.DetailEqDamageLogs)
            {
                var detaiEqDamageLogMap = _mapper.Map<DetailEqDamageLog>(item);
                detaiEqDamageLogMaps.Add(detaiEqDamageLogMap);
            }

            if (!_eqDamageLogRepository.CreateDetailEqDamageLogs(detaiEqDamageLogMaps))
            {
                ModelState.AddModelError("", "Something went wrong while saving");
                return StatusCode(500, ModelState);
            }

            return Ok("Successfully created");
        }
    }
}
