#include "rapidjson/document.h"
#include "rapidjson/writer.h"
#include "rapidjson/stringbuffer.h"

#include <iostream>

#include "stdio.h"

#include "GetSelectedApplicationIds.hpp"
#include "GetModelForElements.hpp"


const char* ExecuteAddonCommand(const char* inputJson)
{
	using namespace rapidjson;

	if (strlen(inputJson) == 0) {
		return 0;
	}

	::printf("Addon::input json: %s\n", inputJson);

	Document input;
	Document output(kObjectType);

	input.Parse(inputJson);

	if (input.HasMember("command")) {
		const std::string cmdName = input["command"].GetString();
		if (cmdName == "API.ExecuteAddOnCommand") {
			if (input.HasMember("parameters")) {
				auto& addonParameters = input["parameters"];

				auto& cmdId = addonParameters["addOnCommandId"];

				const std::string apiCmdName = cmdId["commandName"].GetString();

				Value cmdParameters(kObjectType);
				if (cmdParameters.HasMember("addOnCommandParameters")) {
					cmdParameters = cmdParameters["addOnCommandParameters"];
				}

				if (apiCmdName == "GetSelectedApplicationIds") {
					AddOnCommands::GetSelectedApplicationIds::Execute(cmdParameters, output);
				} else if (apiCmdName == "GetModelForElements") {
					AddOnCommands::GetModelForElements::Execute(cmdParameters, output);
				} else {
					return "";
				}
			}
		}
	}

	rapidjson::StringBuffer buffer;

	buffer.Clear();

	rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);
	output.Accept(writer);

	std::cout << buffer.GetString() << std::endl;

	return strdup (buffer.GetString());
}
