import subprocess
import json
import logging
from typing import List, Dict, Any
from abc import ABC, abstractmethod

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class BaseScanner(ABC):
    @abstractmethod
    async def scan(self, target: str, **kwargs) -> List[Dict[str, Any]]:
        pass
