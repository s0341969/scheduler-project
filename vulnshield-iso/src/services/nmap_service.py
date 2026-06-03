import subprocess
import logging
from typing import List, Dict, Any
from src.services.scanner_base import BaseScanner

logger = logging.getLogger(__name__)

class NmapService(BaseScanner):
    def __init__(self, binary_path: str = 'nmap'):
        self.binary_path = binary_path

    async def scan(self, target: str, **kwargs) -> List[Dict[str, Any]]:
        logger.info(f'Starting Nmap scan on target: {target}')
        try:
            scan_args = kwargs.get('scan_args') or ['-sV', '-T4']
            script_name = kwargs.get('script_name')
            script_args = kwargs.get('script_args')
            timeout = int(kwargs.get('timeout', 300))
            cmd = [self.binary_path, *scan_args, target]
            if script_name:
                cmd.extend(['--script', str(script_name)])
            if script_args:
                cmd.extend(['--script-args', str(script_args)])
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout)
            
            if result.returncode != 0:
                logger.error(f'Nmap failed: {result.stderr}')
                return []
            
            return [{
                'type': 'service_discovery',
                'output': result.stdout,
                'command': cmd,
            }]
        except Exception as e:
            logger.error(f'Nmap error: {str(e)}')
            return []
