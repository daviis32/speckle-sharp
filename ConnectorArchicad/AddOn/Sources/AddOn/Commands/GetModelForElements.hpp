#ifndef GET_MODEL_FOR_ELEMENTS_HPP
#define GET_MODEL_FOR_ELEMENTS_HPP

#include "rapidjson/document.h"

#include "BaseCommand.hpp"
#include "AddonCommand.hpp"


namespace AddOnCommands {


	class GetModelForElements : public BaseCommand {
	public:
		virtual GS::String							GetName() const override;
		virtual GS::ObjectState						Execute(const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;

		//static bool									Execute(const rapidjson::Value& parameters, rapidjson::Document& output);
	};

	class Cmd_GetModelForElements : public AddonCommand
	{
	public:
		virtual std::string GetName() const override;
		virtual bool Execute(const rapidjson::Value&, rapidjson::Document& output) override;

	};

}


#endif