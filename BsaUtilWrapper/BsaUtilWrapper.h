#pragma once

#include <string>;

#include "BSACreate.hpp";
#include "BSAExtract.hpp";
#include "Games.hpp";

using namespace System;

using GameSettings = BSAUtil::GameSettings;
using Games = BSAUtil::Games;

namespace BsaUtilWrapper {
	public struct BsaOptions {
		Games game;
		unsigned int maxSizeGb;
		std::string path;
	};

	public ref class BSA
	{
		BSA() {}

	public:
		void Pack(BsaOptions options);
	};
}
