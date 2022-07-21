#ifndef GET_SELECTED_ELEMENT_IDS_HPP
#define GET_SELECTED_ELEMENT_IDS_HPP

#include "rapidjson/document.h"

#include "BaseCommand.hpp"
#include "AddonCommand.hpp"

namespace AddOnCommands {


  class GetSelectedApplicationIds : public BaseCommand {
  public:
    static GS::Array<API_Guid>          GetSelectedElementGuids();
    virtual GS::String					GetName() const override;
    virtual GS::ObjectState				Execute(const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
    //static bool                         Execute(const rapidjson::Value& parameters, rapidjson::Document& output);
  };

  class Cmd_GetSelectedApplicationIds : public AddonCommand
  {
  public:
      virtual std::string GetName() const override;
      virtual bool Execute(const rapidjson::Value&, rapidjson::Document& output) override;

  private:
      GS::Array<API_Guid> GetSelectedElementGuids();
  };


}


#endif