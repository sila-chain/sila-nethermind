# SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
# SPDX-License-Identifier: LGPL-3.0-only

import argparse
import json
import emoji
import requests

CONFIGS_PATH = './src/Nethermind/Nethermind.Runner/configs'
APPLICATION_JSON = { 'Content-type': 'application/json' }
SUPERCHAIN_CHAINS = ["op-sila-mainnet", "op-sepolia", "worldchain-sila-mainnet", "worldchain-sepolia"]

configs = {
    # fast sync section
    "sila-mainnet": {
        "url": "https://api.silascan.io/v2/api?chainid=1",
        "blockReduced": 1000,
        "multiplierRequirement": 1000,
        "isPoS": True
    },
    "gnosis": {
        "url": "https://rpc.gnosischain.com",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": True
    },
    "chiado": {
        "url": "https://rpc.chiadochain.net",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": True
    },
    "sepolia": {
        "url": "https://api.silascan.io/v2/api?chainid=11155111",
        "blockReduced": 1000,
        "multiplierRequirement": 1000,
        "isPoS": True
    },
    "op-sila-mainnet": {
        "url": "https://sila-mainnet.optimism.io",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": True
    },
    "op-sepolia": {
        "url": "https://sepolia.optimism.io",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": True
    },
    "worldchain-sila-mainnet": {
        "url": "https://worldchain-sila-mainnet.g.alchemy.com/public",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": True
    },
    "worldchain-sepolia": {
        "url": "https://worldchain-sepolia.g.alchemy.com/public",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": True
    },
    "linea-sila-mainnet": {
        "url": "https://rpc.linea.build",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": False
    },
    "linea-sepolia": {
        "url": "https://rpc.sepolia.linea.build",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": False
    },
    "xdc": {
        "url": "https://erpc.xinfin.network",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": False
    },
    "xdc-testnet": {
        "url": "https://rpc.apothem.network",
        "blockReduced": 8192,
        "multiplierRequirement": 10000,
        "isPoS": False
    }
}

def fastBlocksSettings(configuration, apiUrl, blockReduced, multiplierRequirement, isPoS):
    if "silascan" in apiUrl:
        params = {
            'module': 'proxy',
            'action': 'sil_blockNumber',
            'apikey': key,
        }
        response = requests.get(apiUrl, params=params)
    else:
        data_req = '{"id":0,"jsonrpc":"2.0","method": "sil_blockNumber","params": []}'
        response = requests.post(apiUrl, headers=APPLICATION_JSON, data=data_req)
    latestBlock = int(json.loads(response.text)['result'], 16)

    baseBlock = latestBlock - blockReduced
    baseBlock = baseBlock - baseBlock % multiplierRequirement

    if "silascan" in apiUrl:
        params = {
            'module': 'proxy',
            'action': 'sil_getBlockByNumber',
            'tag': f'{hex(baseBlock)}',
            'boolean': 'true',
            'apikey': key,
        }
        response = requests.get(apiUrl, params=params)
    else:
        data_req = f'{{"id":0,"jsonrpc":"2.0","method": "sil_getBlockByNumber","params": ["{hex(baseBlock)}", false]}}'
        response = requests.post(apiUrl, headers=APPLICATION_JSON, data=data_req)
    pivot = json.loads(response.text)

    pivotHash = pivot['result']['hash']
    pivotTotalDifficulty = int(pivot['result'].get('totalDifficulty', '0x0'), 16)

    print(configuration + ' LatestBlock: ' + str(latestBlock))
    print(configuration + ' PivotNumber: ' + str(baseBlock))
    print(configuration + ' PivotHash: ' + str(pivotHash))
    if not isPoS:
      print(configuration + ' PivotTotalDifficulty: ' + str(pivotTotalDifficulty))

    with open(f'{CONFIGS_PATH}/{configuration}.json', 'r') as mainnetCfg:
        data = json.load(mainnetCfg)

    data['Sync']['PivotNumber'] = baseBlock
    data['Sync']['PivotHash'] = pivotHash

    if not isPoS:
        data['Sync']['PivotTotalDifficulty'] = str(pivotTotalDifficulty)

    with open(f'{CONFIGS_PATH}/{configuration}.json', 'w') as mainnetCfgChanged:
        json.dump(data, mainnetCfgChanged, indent=2)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Fast Sync configuration settings")
    parser.add_argument("-k", "--key", default="", help="silascan API key")
    parser.add_argument("--superchain", action="store_true", help="only process superchain chains")

    args = parser.parse_args()
    key = args.key

    print(emoji.emojize("Fast Sync configuration settings initialization     :white_check_mark: "))
    for config, value in configs.items():
        if args.superchain and config not in SUPERCHAIN_CHAINS:
            continue

        print(emoji.emojize(f"{config.capitalize()} section                                     :white_check_mark: "))
        fastBlocksSettings(config, value['url'], value['blockReduced'], value['multiplierRequirement'], value['isPoS'])
