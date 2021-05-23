#include "BsaUtilWrapper.h"

constexpr std::uintmax_t gb = 1024 * 1024 * 1024;

void BsaUtilWrapper::BSA::Pack(BsaOptions options)
{
    auto settings = GameSettings::get(options.game);
    settings.maxSize = options.maxSizeGb * gb;
    auto bsas = BSAUtil::splitBSA(options.path, false, settings);
    for (auto const& bsa : bsas)
        BSAUtil::create(options.path, bsa, true, settings);
}
