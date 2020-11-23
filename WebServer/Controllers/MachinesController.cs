using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DataApiService;
using DataApiService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace WebServer.Controllers
{
    public class MachinesController : Controller
    {
        private readonly ILogger<MachinesController> _logger;
        private IDataManager _dataManager;


        public MachinesController(ILogger<MachinesController> logger, IDataManager dataManager)
        {
            _logger = logger;
            _dataManager = dataManager;
            _dataManager.Auth("demo", "demo1");
        }

        public async Task<IActionResult> Index()
        {
            var machines = await _dataManager.GetItems<Machine>("machines");

            return View(machines);
        }

        [HttpGet]
        public async Task<ActionResult> Create()
        {
            var terminalIds = await _dataManager.GetItems<int>("terminals/free");
            ViewBag.terminalIds = new SelectList(terminalIds);

            var machineModels = await _dataManager.GetItems<MachineModel>("machineModels");
            ViewBag.machineModels = new SelectList(machineModels, "machine_model_id", "machine_model_name");

            return View();
        }

        [HttpPost]
        public ActionResult Create(Machine model)
        {
            _dataManager
                .SendCustomRequest("machines", null, model);

            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var machines = await _dataManager.GetItems<Machine>("machines");
            var currentMachine = machines.FirstOrDefault(x => x.Machine_id == id);

            var machineModels = await _dataManager.GetItems<MachineModel>("machineModels");
            ViewBag.machineModelsWithoutCurrent = new SelectList(machineModels.Where(x => x.machine_model_id !=currentMachine.Model_id) , "machine_model_id", "machine_model_name");

            var terminalIds = await _dataManager.GetItems<int>("terminals/free");
            ViewBag.terminalIds = new SelectList(terminalIds);


            return View(currentMachine);
        }

        [HttpPost]
        public IActionResult Edit(Machine model)
        {
            _dataManager
                .SendCustomRequest($"machines/{model.Machine_id}", null, model, WebRequestMethods.Http.Put);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Delete(int id)
        {
            var machines = await _dataManager.GetItems<Machine>("machines");

            return View(machines.FirstOrDefault(m => m.Machine_id == id));
        }

        [HttpPost]
        public ActionResult DeleteConfirmed(int id)
        {

            _dataManager
                .SendCustomRequest($"machines/{id}", null, null, "DELETE");

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Terminal(int? id)
        {
            var terminalIds = await _dataManager.GetItems<int>("terminals/free");
            ViewBag.terminalIds = new SelectList(terminalIds);

            var machines = await _dataManager.GetItems<Machine>("machines");
            var currentMachine = machines.FirstOrDefault(x => x.Machine_id == id);
            if (currentMachine.Terminal_id == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.currentMachine = new SelectListItem(
                $"{currentMachine.Machine_name} " +
                $"| {currentMachine.Machine_address} " +
                $"| {currentMachine.Machine_model}",
                currentMachine.Machine_id.ToString()
                );

            var commandList = await _dataManager.GetItems<Command>("commands/types");
            ViewBag.commandSelectList = new SelectList(commandList, "Id", "Name");
            ViewBag.commandScriptList = commandList;

            var machineList = machines
                .Select(
                x => new SelectListItem($"{x.Machine_name} | {x.Machine_address} | {x.Machine_model}", x.Machine_id.ToString())
                )
                .Where(x => x.Value != id.ToString()).ToList();
            ViewBag.machineList = machineList;

            var commandForCurrentTerm = await _dataManager.GetItems<CommandListItem>($"terminals/{currentMachine.Terminal_id}/commands");
            ViewBag.commandForCurrentTerm = commandForCurrentTerm;

            return View(currentMachine);
        }

        [HttpPost]
        public IActionResult SendTerminalRequest(IFormCollection col)
        {
            var paramDict = new Dictionary<string, string>
            {
                ["command_id"] = col["command_id"]
            };
            for (int i = 1; i <= 3; i++)
            {
                if (col["parameter" + i.ToString()] != "")
                {
                    paramDict["parameter" + i.ToString()] = col["parameter" + i.ToString()];
                }
            }
            _dataManager.SendCustomRequest($"terminals/{col["Terminal_id"]}/command", paramDict);

            return RedirectToAction("Index");
        }


    }
}
